using System;
using UnityEngine;

namespace UnityBLE.windows
{
    public class WindowsSubscribeCharacteristicCommand : IDisposable
    {
        private readonly string _deviceAddress;
        private readonly string _serviceUuid;
        private readonly string _characteristicUuid;
        private readonly IBleCharacteristic.DataReceivedDelegate _notificationCallback;
        private bool _disposed;
        private bool _isSubscribed;
        private readonly object _lock = new();

        public WindowsSubscribeCharacteristicCommand(
            string deviceAddress,
            string serviceUuid,
            string characteristicUuid,
            IBleCharacteristic.DataReceivedDelegate onValueChanged)
        {
            Debug.Log($"Creating WindowsSubscribeCharacteristicCommand for {characteristicUuid} on {deviceAddress}");
            _deviceAddress = deviceAddress ?? throw new ArgumentNullException(nameof(deviceAddress));
            _serviceUuid = serviceUuid ?? throw new ArgumentNullException(nameof(serviceUuid));
            _characteristicUuid = characteristicUuid ?? throw new ArgumentNullException(nameof(characteristicUuid));
            _notificationCallback = onValueChanged ?? throw new ArgumentNullException(nameof(onValueChanged));
        }

        public void Execute()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(WindowsSubscribeCharacteristicCommand));

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

                    WindowsBleNativePlugin.SubscribeCharacteristic(_deviceAddress, _serviceUuid, _characteristicUuid);
                    _isSubscribed = true;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error subscribing to characteristic {_characteristicUuid}: {ex.Message}");
                    throw;
                }
            }
        }

        private void OnCharacteristicValueReceived(string fromCharacteristicUUID, string data)
        {
            try
            {
                if (fromCharacteristicUUID != _characteristicUuid)
                {
                    return;
                }
                lock (_lock)
                {
                    if (_isSubscribed && _notificationCallback != null)
                    {
                        _notificationCallback.Invoke(data);
                        Debug.Log($"Received notification for {_characteristicUuid}: {data}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error processing characteristic value callback: {ex.Message}");
            }
        }

        public bool IsSubscribed => _isSubscribed;

        public void Dispose()
        {
            lock (_lock)
            {
                if (!_disposed)
                {
                    if (_isSubscribed)
                    {
                        try
                        {
                            WindowsBleNativePlugin.UnsubscribeCharacteristic(_deviceAddress, _serviceUuid, _characteristicUuid);
                            BleDeviceEvents.OnDataReceived -= OnCharacteristicValueReceived;
                            _isSubscribed = false;
                            _disposed = true;
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"Error during cleanup unsubscribe: {ex.Message}");
                        }
                    }
                }
            }
        }
    }
}
