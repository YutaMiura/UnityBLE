using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityBLE.Android
{
    public class SubscribeCommand
    {
        private readonly string _characteristicUuid;
        private readonly string _serviceUuid;
        private readonly string _peripheralUuid;
        private readonly AndroidBleNativePlugin Plugin;

        private bool _disposed;
        private bool _isSubscribed;
        private object _lock = new();
        private readonly IBleCharacteristic.DataReceivedDelegate _notificationCallback;

        public bool IsSubscribed => _isSubscribed;


        internal SubscribeCommand(
            string characteristicUuid,
            string serviceUuid,
            string peripheralUuid,
            AndroidBleNativePlugin plugin,
            IBleCharacteristic.DataReceivedDelegate onValueReceived)
        {
            _characteristicUuid = characteristicUuid;
            _serviceUuid = serviceUuid;
            _peripheralUuid = peripheralUuid;
            Plugin = plugin;
            _notificationCallback = onValueReceived ?? throw new ArgumentNullException(nameof(onValueReceived));
        }

        public void Execute()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(SubscribeCommand));
            }
            lock (_lock)
            {
                if (_isSubscribed)
                {
                    Debug.LogWarning($"Already subscribed to characteristic {_characteristicUuid}");
                    return;
                }

                Debug.Log($"Subscribing to notifications for characteristic {_characteristicUuid}");

                try
                {
                    BleDeviceEvents.OnDataReceived += OnCharacteristicValueReceived;
                    Plugin.Subscribe(_characteristicUuid, _serviceUuid, _peripheralUuid);
                    _isSubscribed = true;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error subscribing to characteristic {_characteristicUuid}: {ex.Message}");
                    throw;
                }
            }
        }

        /// <summary>
        /// <see cref="Execute"/> that completes once the subscription is live on the
        /// device rather than once the request has been accepted. Callers that send a
        /// command straight after subscribing must use this: the CCCD write started by
        /// the request keeps the GATT connection busy, and a command sent inside that
        /// window is rejected and lost. Marks the command subscribed only on success.
        /// </summary>
        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(SubscribeCommand));
            }

            lock (_lock)
            {
                if (_isSubscribed)
                {
                    Debug.LogWarning($"Already subscribed to characteristic {_characteristicUuid}");
                    return;
                }
                BleDeviceEvents.OnDataReceived += OnCharacteristicValueReceived;
            }

            Debug.Log($"Subscribing to notifications for characteristic {_characteristicUuid}");
            try
            {
                var subscribe = Plugin.SubscribeAsync(_characteristicUuid, _serviceUuid, _peripheralUuid);
                // The native layer has no cancel for an in-flight descriptor write, so
                // cancellation stops the wait, not the operation.
                await WaitOrCancelAsync(subscribe, cancellationToken);
                lock (_lock) { _isSubscribed = true; }
            }
            catch (Exception ex)
            {
                // Not subscribed after all — drop the listener so a retry does not
                // register it twice and every notification arrives duplicated.
                BleDeviceEvents.OnDataReceived -= OnCharacteristicValueReceived;
                Debug.LogError($"Error subscribing to characteristic {_characteristicUuid}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Awaits <paramref name="task"/> but gives up when <paramref name="ct"/> is
        /// cancelled. Hand-rolled because Task.WaitAsync does not exist on the
        /// .NET Standard 2.1 profile Unity builds against.
        /// </summary>
        private static async Task WaitOrCancelAsync(Task task, CancellationToken ct)
        {
            if (!ct.CanBeCanceled)
            {
                await task;
                return;
            }

            var cancelled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (ct.Register(() => cancelled.TrySetCanceled(ct)))
            {
                var completed = await Task.WhenAny(task, cancelled.Task);
                if (completed != task)
                {
                    // Abandoning the operation: observe its eventual failure so it does
                    // not surface later as an unobserved task exception.
                    _ = task.ContinueWith(
                        t => { _ = t.Exception; },
                        CancellationToken.None,
                        TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                        TaskScheduler.Default);
                }
                await completed;
            }
        }

        private void OnCharacteristicValueReceived(string from, string data)
        {
            try
            {
                Debug.Log($"Data received for characteristic {from}: {data}");
                if (from != _characteristicUuid)
                {
                    Debug.LogWarning($"Received data for other characteristic {from}, we expected {_characteristicUuid} so skipped.");
                    return;
                }
                lock (_lock)
                {
                    if (_isSubscribed && _notificationCallback != null)
                    {
                        _notificationCallback.Invoke(data);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error processing data for characteristic {_characteristicUuid}: {ex.Message}");
            }
        }

        public async Task UnsubscribeAsync()
        {
            if (_disposed)
            {
                return;
            }
            if (!_isSubscribed)
            {
                _disposed = true;
                return;
            }

            try
            {
                await Plugin.UnsubscribeAsync(_characteristicUuid, _serviceUuid, _peripheralUuid);
            }
            catch (Exception)
            {
                // Best-effort unsubscribe; swallow so the finally-block cleanup always runs.
            }
            finally
            {
                BleDeviceEvents.OnDataReceived -= OnCharacteristicValueReceived;
                lock (_lock)
                {
                    _isSubscribed = false;
                    _disposed = true;
                }
            }
        }
    }
}
