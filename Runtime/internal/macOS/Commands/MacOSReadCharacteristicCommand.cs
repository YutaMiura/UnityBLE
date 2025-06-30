using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityBLE.macOS
{
    public class MacOSReadCharacteristicCommand : IDisposable
    {
        private readonly string _deviceAddress;
        private readonly string _serviceUuid;
        private readonly string _characteristicUuid;
        private TaskCompletionSource<byte[]> _completionSource;
        private bool _disposed;

        public MacOSReadCharacteristicCommand(string deviceAddress, string serviceUuid, string characteristicUuid)
        {
            _deviceAddress = deviceAddress ?? throw new ArgumentNullException(nameof(deviceAddress));
            _serviceUuid = serviceUuid ?? throw new ArgumentNullException(nameof(serviceUuid));
            _characteristicUuid = characteristicUuid ?? throw new ArgumentNullException(nameof(characteristicUuid));
        }

        public async Task<byte[]> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(MacOSReadCharacteristicCommand));

            Debug.Log($"[macOS BLE Command] Reading characteristic {_characteristicUuid} from device {_deviceAddress}...");

            try
            {
                // Set up completion source for async operation
                _completionSource = new TaskCompletionSource<byte[]>();

                // Subscribe to characteristic value callbacks temporarily
                MacOSBleNativePlugin.OnCharacteristicValue += OnCharacteristicValueReceived;

                // Call native plugin to read characteristic
                bool readStarted = MacOSBleNativePlugin.ReadCharacteristic(_deviceAddress, _serviceUuid, _characteristicUuid);

                if (!readStarted)
                {
                    throw new InvalidOperationException($"Failed to start reading characteristic {_characteristicUuid}");
                }

                // Wait for result with timeout
                using var timeoutTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutTokenSource.CancelAfter(5000); // 5 second timeout

                timeoutTokenSource.Token.Register(() => {
                    _completionSource?.TrySetCanceled();
                });

                var result = await _completionSource.Task.ConfigureAwait(false);
                
                Debug.Log($"[macOS BLE Command] Read {result.Length} bytes from characteristic {_characteristicUuid}");
                
                return result;
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning($"[macOS BLE Command] Read operation timed out for characteristic {_characteristicUuid}");
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[macOS BLE Command] Error reading characteristic {_characteristicUuid}: {ex.Message}");
                throw;
            }
            finally
            {
                MacOSBleNativePlugin.OnCharacteristicValue -= OnCharacteristicValueReceived;
                _completionSource = null;
            }
        }

        private void OnCharacteristicValueReceived(string characteristicJson, string valueHex)
        {
            try
            {
                // Parse the characteristic info to check if it's for this characteristic
                var charInfo = JsonUtility.FromJson<CharacteristicValueInfo>(characteristicJson);
                
                if (charInfo?.uuid?.Equals(_characteristicUuid, StringComparison.OrdinalIgnoreCase) == true)
                {
                    // Convert hex string back to byte array
                    byte[] data = new byte[0];
                    if (!string.IsNullOrEmpty(valueHex))
                    {
                        data = HexStringToByteArray(valueHex);
                    }

                    _completionSource?.TrySetResult(data);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[macOS BLE Command] Error processing characteristic value callback: {ex.Message}");
                _completionSource?.TrySetException(ex);
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

        public void Dispose()
        {
            if (!_disposed)
            {
                MacOSBleNativePlugin.OnCharacteristicValue -= OnCharacteristicValueReceived;
                _completionSource?.TrySetCanceled();
                _disposed = true;
            }
        }
    }

    [System.Serializable]
    internal class CharacteristicValueInfo
    {
        public string uuid;
        public string serviceUuid;
        public string deviceAddress;
    }
}