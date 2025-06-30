using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityBLE.iOS
{
    public class iOSUnsubscribeCharacteristicCommand : IDisposable
    {
        private readonly string _deviceAddress;
        private readonly string _serviceUuid;
        private readonly string _characteristicUuid;
        private bool _disposed;

        public iOSUnsubscribeCharacteristicCommand(string deviceAddress, string serviceUuid, string characteristicUuid)
        {
            _deviceAddress = deviceAddress ?? throw new ArgumentNullException(nameof(deviceAddress));
            _serviceUuid = serviceUuid ?? throw new ArgumentNullException(nameof(serviceUuid));
            _characteristicUuid = characteristicUuid ?? throw new ArgumentNullException(nameof(characteristicUuid));
        }

        public Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(iOSUnsubscribeCharacteristicCommand));

            Debug.Log($"[iOS BLE Command] Unsubscribing from notifications for characteristic {_characteristicUuid}");

            try
            {
                // Initialize native plugin if not already done
                if (!iOSBleNativePlugin.Initialize())
                {
                    throw new InvalidOperationException("Failed to initialize iOS BLE native plugin");
                }

                // Call native plugin to disable notifications
                bool success = iOSBleNativePlugin.UnsubscribeCharacteristic(_deviceAddress, _serviceUuid, _characteristicUuid);
                
                if (!success)
                {
                    Debug.LogWarning($"[iOS BLE Command] Failed to unsubscribe from characteristic {_characteristicUuid}, but proceeding to clean up");
                }

                Debug.Log($"[iOS BLE Command] Successfully unsubscribed from characteristic {_characteristicUuid}");
                
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[iOS BLE Command] Error unsubscribing from characteristic {_characteristicUuid}: {ex.Message}");
                return Task.FromException(ex);
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }
    }
}