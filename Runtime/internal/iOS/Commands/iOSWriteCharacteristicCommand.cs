using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityBLE.iOS
{
    public class iOSWriteCharacteristicCommand : IDisposable
    {
        private readonly string _deviceAddress;
        private readonly string _serviceUuid;
        private readonly string _characteristicUuid;
        private bool _disposed;

        public iOSWriteCharacteristicCommand(string deviceAddress, string serviceUuid, string characteristicUuid)
        {
            _deviceAddress = deviceAddress ?? throw new ArgumentNullException(nameof(deviceAddress));
            _serviceUuid = serviceUuid ?? throw new ArgumentNullException(nameof(serviceUuid));
            _characteristicUuid = characteristicUuid ?? throw new ArgumentNullException(nameof(characteristicUuid));
        }

        public Task ExecuteAsync(byte[] data, CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(iOSWriteCharacteristicCommand));

            if (data == null)
                throw new ArgumentNullException(nameof(data));

            Debug.Log($"[iOS BLE Command] Writing {data.Length} bytes to characteristic {_characteristicUuid}...");

            try
            {
                // Initialize native plugin if not already done
                if (!iOSBleNativePlugin.Initialize())
                {
                    throw new InvalidOperationException("Failed to initialize iOS BLE native plugin");
                }

                // Call native plugin to write characteristic (using write with response for reliability)
                bool writeStarted = iOSBleNativePlugin.WriteCharacteristic(_deviceAddress, _serviceUuid, _characteristicUuid, data);

                if (!writeStarted)
                {
                    throw new InvalidOperationException($"Failed to write to characteristic {_characteristicUuid}");
                }

                Debug.Log($"[iOS BLE Command] Successfully wrote {data.Length} bytes to characteristic {_characteristicUuid}");
                
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[iOS BLE Command] Error writing characteristic {_characteristicUuid}: {ex.Message}");
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