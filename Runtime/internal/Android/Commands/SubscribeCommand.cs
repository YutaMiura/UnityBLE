using System;
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
            if (_disposed) return;
            if (!_isSubscribed)
            {
                _disposed = true;
                return;
            }

            try
            {
                await Plugin.UnsubscribeAsync(_characteristicUuid, _serviceUuid, _peripheralUuid);
                BleDeviceEvents.OnDataReceived -= OnCharacteristicValueReceived;
                lock (_lock)
                {
                    _isSubscribed = false;
                    _disposed = true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error unsubscribing from characteristic {_characteristicUuid}: {ex.Message}");
            }
        }
    }
}