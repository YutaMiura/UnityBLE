using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityBLE.macOS
{
    public class MacOSUnsubscribeCharacteristicCommand : IDisposable
    {
        private readonly string _deviceAddress;
        private readonly string _serviceUuid;
        private readonly string _characteristicUuid;
        private bool _disposed;

        public MacOSUnsubscribeCharacteristicCommand(string deviceAddress, string serviceUuid, string characteristicUuid)
        {
            _deviceAddress = deviceAddress ?? throw new ArgumentNullException(nameof(deviceAddress));
            _serviceUuid = serviceUuid ?? throw new ArgumentNullException(nameof(serviceUuid));
            _characteristicUuid = characteristicUuid ?? throw new ArgumentNullException(nameof(characteristicUuid));
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(MacOSUnsubscribeCharacteristicCommand));

            Debug.Log($"[macOS BLE Command] Unsubscribing from notifications for characteristic {_characteristicUuid}");

            try
            {
                // Call native plugin to disable notifications
                bool success = MacOSBleNativePlugin.UnsubscribeCharacteristic(_deviceAddress, _serviceUuid, _characteristicUuid);
                
                if (!success)
                {
                    Debug.LogWarning($"[macOS BLE Command] Failed to unsubscribe from characteristic {_characteristicUuid}, but proceeding to clean up");
                }

                await Task.Delay(200, cancellationToken);

                Debug.Log($"[macOS BLE Command] Successfully unsubscribed from characteristic {_characteristicUuid}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[macOS BLE Command] Error unsubscribing from characteristic {_characteristicUuid}: {ex.Message}");
                throw;
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