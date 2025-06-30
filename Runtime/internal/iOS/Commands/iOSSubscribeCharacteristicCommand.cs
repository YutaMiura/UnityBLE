using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityBLE.iOS
{
    public class iOSSubscribeCharacteristicCommand : IDisposable
    {
        private readonly string _deviceAddress;
        private readonly string _serviceUuid;
        private readonly string _characteristicUuid;
        private Action<byte[]> _notificationCallback;
        private bool _disposed;
        private bool _isSubscribed;

        public iOSSubscribeCharacteristicCommand(string deviceAddress, string serviceUuid, string characteristicUuid)
        {
            _deviceAddress = deviceAddress ?? throw new ArgumentNullException(nameof(deviceAddress));
            _serviceUuid = serviceUuid ?? throw new ArgumentNullException(nameof(serviceUuid));
            _characteristicUuid = characteristicUuid ?? throw new ArgumentNullException(nameof(characteristicUuid));
        }

        public Task ExecuteAsync(Action<byte[]> onValueChanged, CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(iOSSubscribeCharacteristicCommand));

            if (_isSubscribed)
            {
                Debug.LogWarning($"[iOS BLE Command] Already subscribed to characteristic {_characteristicUuid}");
                return Task.CompletedTask;
            }

            if (onValueChanged == null)
            {
                throw new ArgumentNullException(nameof(onValueChanged));
            }

            Debug.Log($"[iOS BLE Command] Subscribing to notifications for characteristic {_characteristicUuid}");

            try
            {
                // Initialize native plugin if not already done
                if (!iOSBleNativePlugin.Initialize())
                {
                    throw new InvalidOperationException("Failed to initialize iOS BLE native plugin");
                }

                // Store the notification callback
                _notificationCallback = onValueChanged;

                // Subscribe to characteristic value callbacks
                iOSBleNativePlugin.OnCharacteristicValue += OnCharacteristicValueReceived;

                // Call native plugin to enable notifications
                bool success = iOSBleNativePlugin.SubscribeCharacteristic(_deviceAddress, _serviceUuid, _characteristicUuid);

                if (!success)
                {
                    throw new InvalidOperationException($"Failed to subscribe to characteristic {_characteristicUuid} notifications");
                }

                _isSubscribed = true;
                Debug.Log($"[iOS BLE Command] Successfully subscribed to characteristic {_characteristicUuid}");
                
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[iOS BLE Command] Error subscribing to characteristic {_characteristicUuid}: {ex.Message}");

                // Clean up on error
                iOSBleNativePlugin.OnCharacteristicValue -= OnCharacteristicValueReceived;
                _notificationCallback = null;

                return Task.FromException(ex);
            }
        }

        private void OnCharacteristicValueReceived(string characteristicJson, string valueHex)
        {
            try
            {
                // Convert hex string back to byte array
                byte[] data = Array.Empty<byte>();
                if (!string.IsNullOrEmpty(valueHex))
                {
                    data = HexStringToByteArray(valueHex);
                }

                // If this is a notification and we're subscribed
                if (_isSubscribed && _notificationCallback != null)
                {
                    _notificationCallback.Invoke(data);
                    Debug.Log($"[iOS BLE Command] Received notification for {_characteristicUuid}: {data.Length} bytes");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[iOS BLE Command] Error processing characteristic value callback: {ex.Message}");
            }
        }

        private static byte[] HexStringToByteArray(string hex)
        {
            if (string.IsNullOrEmpty(hex) || hex.Length % 2 != 0)
                return new byte[0];

            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < hex.Length; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
            return bytes;
        }

        public bool IsSubscribed => _isSubscribed;

        public void Dispose()
        {
            if (!_disposed)
            {
                if (_isSubscribed)
                {
                    // Try to unsubscribe cleanly
                    try
                    {
                        iOSBleNativePlugin.UnsubscribeCharacteristic(_deviceAddress, _serviceUuid, _characteristicUuid);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[iOS BLE Command] Error during cleanup unsubscribe: {ex.Message}");
                    }
                }

                iOSBleNativePlugin.OnCharacteristicValue -= OnCharacteristicValueReceived;
                _notificationCallback = null;
                _isSubscribed = false;
                _disposed = true;
            }
        }
    }
}