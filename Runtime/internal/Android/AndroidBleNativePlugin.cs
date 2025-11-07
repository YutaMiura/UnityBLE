
using System;
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
            BleManagerClass = new AndroidJavaClass(CLASS_BLE_MANAGER);
            BleManagerInstance = BleManagerClass.CallStatic<AndroidJavaObject>("getInstance");
            CreateHandlers();
            eventReceiver.listener.OnScanResult += ScanResultCallback;
            eventReceiver.listener.OnStopScanResult += StopScanResultCallback;
            eventReceiver.listener.OnReadResult += ReadResultCallback;
            eventReceiver.listener.OnWriteResult += WriteResultCallback;
            eventReceiver.listener.OnUnsubscribeResult += UnsubscribeResultCallback;
        }

        private TaskCompletionSource<int> startScanTask;
        private TaskCompletionSource<int> stopScanTask;
        private TaskCompletionSource<string> readTask;
        private TaskCompletionSource<int> writeTask;
        private TaskCompletionSource<int> unsubscribeTask;

        private readonly object startScanLock = new object();
        private readonly object stopScanLock = new object();
        private readonly object readLock = new object();
        private readonly object writeLock = new object();
        private readonly object unsubscribeLock = new object();

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
            BleManagerInstance.Call(METHOD_NAME_START_SCAN, names, serviceUUIDs);

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

        public Task StopScanAsync()
        {
            lock (stopScanLock)
            {
                if (stopScanTask != null && !stopScanTask.Task.IsCompleted)
                {
                    Debug.LogWarning("StopScanAsync called while a previous StopScanAsync is still in progress.");
                    return stopScanTask.Task;
                }
                stopScanTask = new TaskCompletionSource<int>();
            }

            Debug.Log("AndroidBleNativePlugin.StopScanAsync() called.");
            BleManagerInstance.Call(METHOD_NAME_STOP_SCAN);
            Debug.Log("AndroidBleNativePlugin.StopScanAsync() Native method called.");

            return stopScanTask.Task;
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
            BleManagerInstance.Call(METHOD_NAME_CONNECT, device.UUID);
        }

        public void Disconnect(IBlePeripheral device)
        {
            BleManagerInstance.Call(METHOD_NAME_DISCONNECT, device.UUID);
        }

        public void DiscoveryServices(IBlePeripheral device)
        {
            BleManagerInstance.Call(METHOD_NAME_DISCOVERY_SERVICES, device.UUID);
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

            var result = BleManagerInstance.Call<int>(METHOD_NAME_READ, characteristic.Uuid, characteristic.serviceUUID, characteristic.peripheralUUID);

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

        public Task WriteAsync(IBleCharacteristic characteristic, byte[] data)
        {
            lock (writeLock)
            {
                if (writeTask != null && !writeTask.Task.IsCompleted)
                {
                    Debug.LogWarning("WriteAsync called while a previous WriteAsync is still in progress.");
                    return writeTask.Task;
                }
                writeTask = new TaskCompletionSource<int>();
            }

            var result = BleManagerInstance.Call<int>(METHOD_NAME_WRITE, characteristic.Uuid, characteristic.serviceUUID, characteristic.peripheralUUID, data);
            if (result != 0)
            {
                lock (writeLock)
                {
                    writeTask.TrySetException(new Exception($"Failed to write characteristic {characteristic.Uuid} of peripheral {characteristic.peripheralUUID}, error code: {result}"));
                }
            }
            return writeTask.Task;
        }

        private void WriteResultCallback(string from, int status)
        {
            TaskCompletionSource<int> taskToComplete = null;

            lock (writeLock)
            {
                if (writeTask == null || writeTask.Task.IsCompleted)
                {
                    Debug.LogWarning($"WriteResultCallback called but no pending task (from: {from}, status: {status})");
                    return;
                }
                taskToComplete = writeTask;
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
            var result = BleManagerInstance.Call<int>(METHOD_NAME_SUBSCRIBE, characteristicUuid, serviceUuid, peripheralUuid);
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

            var result = BleManagerInstance.Call<int>(METHOD_NAME_UNSUBSCRIBE, characteristicUuid, serviceUuid, peripheralUuid);

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
            BleManagerClass?.Dispose();
            BleManagerInstance?.Dispose();
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
                eventReceiver.Dispose();
                eventReceiver = null;
            }
        }

    }
}