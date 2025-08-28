using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityBLE.apple
{
    public class AppleReadCharacteristicCommand : IDisposable
    {
        private readonly string _deviceAddress;
        private readonly string _serviceUuid;
        private readonly string _characteristicUuid;
        private TaskCompletionSource<string> _completionSource;
        private bool _disposed;

        public AppleReadCharacteristicCommand(string deviceAddress, string serviceUuid, string characteristicUuid)
        {
            _deviceAddress = deviceAddress ?? throw new ArgumentNullException(nameof(deviceAddress));
            _serviceUuid = serviceUuid ?? throw new ArgumentNullException(nameof(serviceUuid));
            _characteristicUuid = characteristicUuid ?? throw new ArgumentNullException(nameof(characteristicUuid));
        }

        public async Task<string> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(AppleReadCharacteristicCommand));

            Debug.Log($"Reading characteristic {_characteristicUuid} from device {_deviceAddress}...");

            try
            {
                // Set up completion source for async operation
                _completionSource = new TaskCompletionSource<string>();

                // Subscribe to characteristic value callbacks temporarily
                BleDeviceEvents.OnDataReceived += OnCharacteristicValueReceived;

                // Call native plugin to read characteristic
                AppleBleNativePlugin.ReadCharacteristic(_deviceAddress, _serviceUuid, _characteristicUuid);

                var result = await _completionSource.Task.ConfigureAwait(false);

                Debug.Log($"Read {result.Length} bytes from characteristic {_characteristicUuid}");

                return result;
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning($"Read operation timed out for characteristic {_characteristicUuid}");
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error reading characteristic {_characteristicUuid}: {ex.Message}");
                throw;
            }
            finally
            {
                BleDeviceEvents.OnDataReceived -= OnCharacteristicValueReceived;
                _completionSource = null;
            }
        }

        private void OnCharacteristicValueReceived(string characteristicUuid, string valueHex)
        {
            if (_characteristicUuid != characteristicUuid)
            {
                return;
            }
            // Convert hex string back to byte array
            byte[] bytes = new byte[0];
            if (!string.IsNullOrEmpty(valueHex))
            {
                bytes = HexStringToByteArray(valueHex);
            }

            var str = System.Text.Encoding.UTF8.GetString(bytes);

            _completionSource?.TrySetResult(str);
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
                BleDeviceEvents.OnDataReceived -= OnCharacteristicValueReceived;
                _completionSource?.TrySetCanceled();
                _disposed = true;
            }
        }
    }
}