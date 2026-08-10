
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityBLE.Android
{
    internal partial class AndroidBleNativePlugin : IDisposable
    {
        private AndroidJavaClass BleManagerClass;
        private AndroidJavaObject BleManagerInstance;

        internal AndroidBleNativePlugin()
        {
            // JNI construction (AndroidJavaClass / getInstance) must run on the Unity
            // main thread; from a background thread it silently fails. See UnityBleMainThread.
            UnityBleMainThread.Run(() =>
            {
                BleManagerClass = new AndroidJavaClass(CLASS_BLE_MANAGER);
                BleManagerInstance = BleManagerClass.CallStatic<AndroidJavaObject>("getInstance");
                CreateHandlers();
            });
            eventReceiver.listener.OnScanResult += ScanResultCallback;
            eventReceiver.listener.OnStopScanResult += StopScanResultCallback;
            eventReceiver.listener.OnReadResult += ReadResultCallback;
            eventReceiver.listener.OnWriteResult += WriteResultCallback;
            eventReceiver.listener.OnUnsubscribeResult += UnsubscribeResultCallback;
            eventReceiver.listener.OnDescriptorWriteResult += DescriptorWriteResultCallback;
        }

        private TaskCompletionSource<int> startScanTask;
        private TaskCompletionSource<int> stopScanTask;
        private TaskCompletionSource<string> readTask;
        private TaskCompletionSource<int> unsubscribeTask;
        private const int StopScanTimeoutMillis = 2000;

        // Bounds a single characteristic write. The native side answers as soon as it has
        // handed the value to the GATT stack, so this only fires when that answer is lost
        // — and it must, because writes to one characteristic are chained and a write that
        // never completed would stall every later one behind it.
        private const int WriteTimeoutMillis = 2000;

        private readonly object startScanLock = new object();
        private readonly object stopScanLock = new object();
        private readonly object readLock = new object();
        // Writes are tracked per characteristic, not in one shared slot. With a single slot
        // a write issued while another was still in flight was NOT SENT AT ALL — the call
        // returned the earlier write's task and the new value was silently dropped — and
        // the completion callback, which does carry the characteristic it belongs to, was
        // applied to whatever happened to be pending.
        //
        // writeTasks holds the write currently awaiting its native completion; writeQueues
        // holds the tail of each characteristic's chain so overlapping writes are sent one
        // after another instead of racing or being lost.
        private readonly object writeLock = new object();
        private readonly Dictionary<string, TaskCompletionSource<int>> writeTasks =
            new Dictionary<string, TaskCompletionSource<int>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Task> writeQueues =
            new Dictionary<string, Task>(StringComparer.OrdinalIgnoreCase);
        private readonly object unsubscribeLock = new object();

        // Pending SubscribeAsync calls, keyed by characteristic UUID. Keyed rather
        // than single-slot like the read/write tasks above because two
        // characteristics can be subscribed concurrently and each waits for its own
        // CCCD write to complete.
        private readonly object subscribeLock = new object();
        private readonly Dictionary<string, TaskCompletionSource<int>> subscribeTasks =
            new Dictionary<string, TaskCompletionSource<int>>(StringComparer.OrdinalIgnoreCase);

        public Task StartScanAsync(ScanFilter filter)
        {
            lock (startScanLock)
            {
                if (startScanTask != null && !startScanTask.Task.IsCompleted)
                {
                    Debug.LogWarning("StartScanAsync called while a previous StartScanAsync is still in progress.");
                    return startScanTask.Task;
                }
                startScanTask = new TaskCompletionSource<int>();
            }

            var serviceUUIDs = filter.ServiceUuids;
            var names = new String[] {
                filter.Name
            };
            UnityBleMainThread.Run(() => BleManagerInstance.Call(METHOD_NAME_START_SCAN, names, serviceUUIDs));

            return startScanTask.Task;
        }

        void ScanResultCallback(int code)
        {
            TaskCompletionSource<int> taskToComplete = null;

            lock (startScanLock)
            {
                if (startScanTask == null || startScanTask.Task.IsCompleted)
                {
                    Debug.LogWarning($"ScanResultCallback called but no pending task (code: {code})");
                    return;
                }
                taskToComplete = startScanTask;
            }

            // Complete the task outside the lock to avoid potential deadlocks
            if (code == 0)
            {
                taskToComplete.TrySetResult(code);
            }
            else if (code == 1)
            {
                taskToComplete.TrySetException(new BleUnsupported());
            }
            else if (code == 2)
            {
                taskToComplete.TrySetException(new BleUnAuthorized());
            }
            else if (code == 3)
            {
                taskToComplete.TrySetException(new BleUnAuthorized());
            }
            else
            {
                taskToComplete.TrySetException(new Exception($"Unknown error code: {code}"));
            }
        }

        public async Task StopScanAsync()
        {
            TaskCompletionSource<int> pendingTask;
            var shouldInvokeNative = false;
            lock (stopScanLock)
            {
                if (stopScanTask != null && !stopScanTask.Task.IsCompleted)
                {
                    Debug.LogWarning("StopScanAsync called while a previous StopScanAsync is still in progress.");
                    pendingTask = stopScanTask;
                }
                else
                {
                    stopScanTask = new TaskCompletionSource<int>();
                    pendingTask = stopScanTask;
                    shouldInvokeNative = true;
                }
            }

            // Only the first (non-coalesced) caller invokes native stopScan; concurrent
            // callers piggyback on the same pending task. JNI must run on the main thread.
            if (shouldInvokeNative)
            {
                Debug.Log("AndroidBleNativePlugin.StopScanAsync() called.");
                UnityBleMainThread.Run(() => BleManagerInstance.Call(METHOD_NAME_STOP_SCAN));
                Debug.Log("AndroidBleNativePlugin.StopScanAsync() Native method called.");
            }

            var completedTask = await Task.WhenAny(pendingTask.Task, Task.Delay(StopScanTimeoutMillis));
            if (completedTask == pendingTask.Task)
            {
                await pendingTask.Task;
                return;
            }

            Debug.LogWarning($"StopScanAsync timed out after {StopScanTimeoutMillis}ms. Continuing as stopped.");
            pendingTask.TrySetResult(0);
        }

        private void StopScanResultCallback(int code)
        {
            TaskCompletionSource<int> taskToComplete = null;

            lock (stopScanLock)
            {
                if (stopScanTask == null || stopScanTask.Task.IsCompleted)
                {
                    Debug.LogWarning($"StopScanResultCallback called but no pending task (code: {code})");
                    return;
                }
                taskToComplete = stopScanTask;
            }

            // Complete the task outside the lock to avoid potential deadlocks
            Debug.Log("AndroidBleNativePlugin.OnStopScanResult called with code: " + code);
            if (code == 0)
            {
                taskToComplete.TrySetResult(code);
            }
            else if (code == 1)
            {
                taskToComplete.TrySetException(new BleUnsupported());
            }
            else if (code == 2)
            {
                taskToComplete.TrySetException(new BleUnAuthorized());
            }
            else if (code == 3)
            {
                taskToComplete.TrySetException(new BleUnAuthorized());
            }
            else
            {
                taskToComplete.TrySetException(new Exception($"Unknown error code: {code}"));
            }
        }

        public void Connect(IBlePeripheral device)
        {
            UnityBleMainThread.Run(() => BleManagerInstance.Call(METHOD_NAME_CONNECT, device.UUID));
        }

        public void Disconnect(IBlePeripheral device)
        {
            UnityBleMainThread.Run(() => BleManagerInstance.Call(METHOD_NAME_DISCONNECT, device.UUID));
        }

        public void DiscoveryServices(IBlePeripheral device)
        {
            UnityBleMainThread.Run(() => BleManagerInstance.Call(METHOD_NAME_DISCOVERY_SERVICES, device.UUID));
        }

        public Task<string> ReadAsync(IBleCharacteristic characteristic)
        {
            lock (readLock)
            {
                if (readTask != null && !readTask.Task.IsCompleted)
                {
                    Debug.LogWarning("ReadAsync called while a previous ReadAsync is still in progress.");
                    return readTask.Task;
                }
                readTask = new TaskCompletionSource<string>();
            }

            var result = UnityBleMainThread.Run(() => BleManagerInstance.Call<int>(METHOD_NAME_READ, characteristic.Uuid, characteristic.serviceUUID, characteristic.peripheralUUID));

            if (result != 0)
            {
                lock (readLock)
                {
                    readTask.TrySetException(new Exception($"Failed to read characteristic {characteristic.Uuid} of peripheral {characteristic.peripheralUUID}, error code: {result}"));
                }
            }

            return readTask.Task;
        }

        private void ReadResultCallback(string from, int status, string data)
        {
            TaskCompletionSource<string> taskToComplete = null;

            lock (readLock)
            {
                if (readTask == null || readTask.Task.IsCompleted)
                {
                    Debug.LogWarning($"ReadResultCallback called but no pending task (from: {from}, status: {status})");
                    return;
                }
                taskToComplete = readTask;
            }

            // Complete the task outside the lock to avoid potential deadlocks
            if (status == 0)
            {
                taskToComplete.TrySetResult(data);
            }
            else
            {
                taskToComplete.TrySetException(new Exception($"Unknown error code: {status}"));
            }
        }

        /// <summary>
        /// Writes to a characteristic, queueing behind any write to that same
        /// characteristic that has not completed yet. Every call reaches the device: the
        /// returned task completes when THIS value has been written.
        /// </summary>
        public Task WriteAsync(IBleCharacteristic characteristic, byte[] data)
        {
            lock (writeLock)
            {
                writeQueues.TryGetValue(characteristic.Uuid, out var previous);
                var queued = WriteAfterAsync(previous, characteristic, data);
                writeQueues[characteristic.Uuid] = queued;
                return queued;
            }
        }

        private async Task WriteAfterAsync(Task previous, IBleCharacteristic characteristic, byte[] data)
        {
            // Yield before anything else. WriteAsync starts this method inside writeLock and
            // an async method runs synchronously up to its first suspension — including a
            // completed `await previous`. UnityBleMainThread.Run below BLOCKS when called
            // off the main thread, so without this it would block while holding writeLock,
            // and the main thread would deadlock against it the moment a write callback
            // arrived and tried to take the same lock.
            await Task.Yield();

            if (previous != null)
            {
                // Outcome ignored on purpose: an earlier write's failure belongs to whoever
                // issued it, and must not cancel this one.
                try { await previous; } catch { /* observed by its own caller */ }
            }

            var uuid = characteristic.Uuid;
            var task = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (writeLock)
            {
                writeTasks[uuid] = task;
            }

            try
            {
                var result = UnityBleMainThread.Run(() => BleManagerInstance.Call<int>(
                    METHOD_NAME_WRITE, uuid, characteristic.serviceUUID, characteristic.peripheralUUID, data));
                if (result != 0)
                {
                    throw new Exception(
                        $"Failed to write characteristic {uuid} of peripheral {characteristic.peripheralUUID}, error code: {result}");
                }

                var completed = await Task.WhenAny(task.Task, Task.Delay(WriteTimeoutMillis));
                if (completed != task.Task)
                {
                    throw new TimeoutException(
                        $"Write to characteristic {uuid} of peripheral {characteristic.peripheralUUID} was not acknowledged within {WriteTimeoutMillis}ms.");
                }

                await task.Task;
            }
            finally
            {
                // Drop the registration however this ended, so a late callback cannot
                // complete a write that already gave up and the next write starts clean.
                lock (writeLock)
                {
                    if (writeTasks.TryGetValue(uuid, out var registered) && ReferenceEquals(registered, task))
                    {
                        writeTasks.Remove(uuid);
                    }
                }
            }
        }

        private void WriteResultCallback(string from, int status)
        {
            TaskCompletionSource<int> taskToComplete;

            lock (writeLock)
            {
                // Matched on `from`: the callback names the characteristic it belongs to,
                // and completing whatever was pending instead let one characteristic's
                // result finish another's write.
                if (!writeTasks.TryGetValue(from, out taskToComplete) || taskToComplete.Task.IsCompleted)
                {
                    Debug.LogWarning($"WriteResultCallback called but no pending task (from: {from}, status: {status})");
                    return;
                }
                writeTasks.Remove(from);
            }

            // Complete the task outside the lock to avoid potential deadlocks
            if (status == 0)
            {
                taskToComplete.TrySetResult(status);
            }
            else
            {
                taskToComplete.TrySetException(new Exception($"Unknown error code: {status}"));
            }
        }
        public void Subscribe(string characteristicUuid, string serviceUuid, string peripheralUuid)
        {
            if (string.IsNullOrEmpty(characteristicUuid) || string.IsNullOrEmpty(serviceUuid) || string.IsNullOrEmpty(peripheralUuid))
            {
                throw new ArgumentException($"characteristicUuid, serviceUuid, and peripheralUuid must be non-empty {characteristicUuid} {serviceUuid} {peripheralUuid}");
            }
            var result = UnityBleMainThread.Run(() => BleManagerInstance.Call<int>(METHOD_NAME_SUBSCRIBE, characteristicUuid, serviceUuid, peripheralUuid));
            if (result == 0)
            {
                return;
            }
            else if (result == 2)
            {
                throw new NotSupportedException($"Failed to subscribe to characteristic {characteristicUuid} of peripheral {peripheralUuid}, error code: {result}");
            }
            else
            {
                throw new Exception($"Failed to subscribe to characteristic {characteristicUuid} of peripheral {peripheralUuid}, error code: {result}");
            }
        }

        /// <summary>
        /// Enables notifications and completes when the subscription has actually
        /// taken effect on the device — i.e. when the CCCD descriptor write finishes.
        ///
        /// <para><see cref="Subscribe"/> only reports that the native layer ACCEPTED
        /// the request. The descriptor write it starts is an asynchronous GATT
        /// operation, and Android's stack runs one operation at a time: anything sent
        /// during that window is rejected as busy and never reaches the device. A
        /// caller that sends its first command right after Subscribe therefore loses
        /// it, with no error on any layer. Awaiting this instead closes that window.</para>
        /// </summary>
        public Task SubscribeAsync(string characteristicUuid, string serviceUuid, string peripheralUuid)
        {
            TaskCompletionSource<int> task;
            lock (subscribeLock)
            {
                if (subscribeTasks.TryGetValue(characteristicUuid, out var pending) && !pending.Task.IsCompleted)
                {
                    Debug.LogWarning($"SubscribeAsync called while a previous SubscribeAsync for {characteristicUuid} is still in progress.");
                    return pending.Task;
                }
                task = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
                subscribeTasks[characteristicUuid] = task;
            }

            try
            {
                Subscribe(characteristicUuid, serviceUuid, peripheralUuid);
            }
            catch (Exception)
            {
                // The request was refused outright, so no descriptor-write callback is
                // coming; drop the registration and let the caller see the throw.
                lock (subscribeLock) { subscribeTasks.Remove(characteristicUuid); }
                throw;
            }

            return task.Task;
        }

        private void DescriptorWriteResultCallback(string from, string descriptor, int status)
        {
            TaskCompletionSource<int> taskToComplete;
            lock (subscribeLock)
            {
                // Also fires for the CCCD disable issued by unsubscribe, which nobody
                // awaits — no pending entry simply means this one is not ours.
                if (!subscribeTasks.TryGetValue(from, out taskToComplete) || taskToComplete.Task.IsCompleted)
                {
                    return;
                }
                subscribeTasks.Remove(from);
            }

            // Completed outside the lock to avoid running continuations under it.
            if (status == 0)
            {
                taskToComplete.TrySetResult(status);
            }
            else
            {
                taskToComplete.TrySetException(new Exception(
                    $"Failed to enable notifications on characteristic {from} (descriptor {descriptor}), status: {status}"));
            }
        }

        public Task UnsubscribeAsync(string characteristicUuid, string serviceUuid, string peripheralUuid)
        {
            if (string.IsNullOrEmpty(characteristicUuid) || string.IsNullOrEmpty(serviceUuid) || string.IsNullOrEmpty(peripheralUuid))
            {
                throw new ArgumentException($"characteristicUuid, serviceUuid, and peripheralUuid must be non-empty {characteristicUuid} {serviceUuid} {peripheralUuid}");
            }

            lock (unsubscribeLock)
            {
                if (unsubscribeTask != null && !unsubscribeTask.Task.IsCompleted)
                {
                    Debug.LogWarning("UnsubscribeAsync called while a previous UnsubscribeAsync is still in progress.");
                    return unsubscribeTask.Task;
                }
                unsubscribeTask = new TaskCompletionSource<int>();
            }

            var result = UnityBleMainThread.Run(() => BleManagerInstance.Call<int>(METHOD_NAME_UNSUBSCRIBE, characteristicUuid, serviceUuid, peripheralUuid));

            if (result == 2)
            {
                lock (unsubscribeLock)
                {
                    unsubscribeTask.TrySetException(new NotSupportedException($"Failed to subscribe to characteristic {characteristicUuid} of peripheral {peripheralUuid}, error code: {result}"));
                }
            }
            else if (result != 0)
            {
                lock (unsubscribeLock)
                {
                    unsubscribeTask.TrySetException(new Exception($"Failed to unsubscribe to characteristic {characteristicUuid} of peripheral {peripheralUuid}, error code: {result}"));
                }
            }
            else
            {
                // Native accepted the unsubscribe request. The native side issues the
                // CCCD-disable descriptor write but never emits a success callback
                // (onDescriptorWrite is informational only and notifyOnUnSubscribe fires
                // only on failure), so there is no completion signal to await — waiting
                // would hang DisconnectAsync forever. Complete immediately, mirroring the
                // synchronous fire-and-forget Subscribe() above. Complete outside the lock
                // to avoid running continuations under it (see WriteResultCallback).
                unsubscribeTask.TrySetResult(result);
            }

            return unsubscribeTask.Task;
        }

        private void UnsubscribeResultCallback(string from, int status)
        {
            TaskCompletionSource<int> taskToComplete = null;

            lock (unsubscribeLock)
            {
                if (unsubscribeTask == null || unsubscribeTask.Task.IsCompleted)
                {
                    Debug.LogWarning($"UnsubscribeResultCallback called but no pending task (from: {from}, status: {status})");
                    return;
                }
                taskToComplete = unsubscribeTask;
            }

            // Complete the task outside the lock to avoid potential deadlocks
            if (status == 0)
            {
                taskToComplete.TrySetResult(status);
            }
            else
            {
                taskToComplete.TrySetException(new Exception($"Unknown error code: {status}"));
            }
        }

        public void Dispose()
        {
            UnityBleMainThread.Run(() =>
            {
                BleManagerClass?.Dispose();
                BleManagerInstance?.Dispose();
            });
            BleManagerClass = null;
            BleManagerInstance = null;
            if (logReceiver != null)
            {
                logReceiver.Dispose();
                logReceiver = null;
            }
            if (eventReceiver != null)
            {
                eventReceiver.listener.OnScanResult -= ScanResultCallback;
                eventReceiver.listener.OnStopScanResult -= StopScanResultCallback;
                eventReceiver.listener.OnReadResult -= ReadResultCallback;
                eventReceiver.listener.OnWriteResult -= WriteResultCallback;
                eventReceiver.listener.OnUnsubscribeResult -= UnsubscribeResultCallback;
                eventReceiver.listener.OnDescriptorWriteResult -= DescriptorWriteResultCallback;
                eventReceiver.Dispose();
                eventReceiver = null;
            }
        }

    }
}