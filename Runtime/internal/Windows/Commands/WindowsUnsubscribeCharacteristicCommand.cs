using System;
using UnityEngine;

namespace UnityBLE.windows
{
    public class WindowsUnsubscribeCharacteristicCommand : IDisposable
    {
        private readonly string _deviceAddress;
        private readonly string _serviceUuid;
        private readonly string _characteristicUuid;
        private bool _disposed;

        public WindowsUnsubscribeCharacteristicCommand(string deviceAddress, string serviceUuid, string characteristicUuid)
        {
            _deviceAddress = deviceAddress ?? throw new ArgumentNullException(nameof(deviceAddress));
            _serviceUuid = serviceUuid ?? throw new ArgumentNullException(nameof(serviceUuid));
            _characteristicUuid = characteristicUuid ?? throw new ArgumentNullException(nameof(characteristicUuid));
        }

        public void Execute()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(WindowsUnsubscribeCharacteristicCommand));

            Debug.Log($"Unsubscribing from notifications for characteristic {_characteristicUuid}");

            try
            {
                WindowsBleNativePlugin.UnsubscribeCharacteristic(_deviceAddress, _serviceUuid, _characteristicUuid);

                Debug.Log($"Successfully unsubscribed from characteristic {_characteristicUuid}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error unsubscribing from characteristic {_characteristicUuid}: {ex.Message}");
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
