using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityBLE.macOS
{
    public class MacOSWriteCharacteristicCommand : IDisposable
    {
        private readonly string _deviceAddress;
        private readonly string _serviceUuid;
        private readonly string _characteristicUuid;
        private readonly byte[] _data;
        private readonly bool _withResponse;
        private TaskCompletionSource<bool> _completionSource;
        private bool _disposed;

        public MacOSWriteCharacteristicCommand(string deviceAddress, string serviceUuid, string characteristicUuid, byte[] data, bool withResponse)
        {
            _deviceAddress = deviceAddress ?? throw new ArgumentNullException(nameof(deviceAddress));
            _serviceUuid = serviceUuid ?? throw new ArgumentNullException(nameof(serviceUuid));
            _characteristicUuid = characteristicUuid ?? throw new ArgumentNullException(nameof(characteristicUuid));
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _withResponse = withResponse;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(MacOSWriteCharacteristicCommand));

            Debug.Log($"[macOS BLE Command] Writing {_data.Length} bytes to characteristic {_characteristicUuid} {(_withResponse ? "with" : "without")} response...");

            try
            {
                if (_withResponse)
                {
                    // Set up completion source for write with response
                    _completionSource = new TaskCompletionSource<bool>();
                    
                    // Subscribe to characteristic value callbacks temporarily for write confirmation
                    MacOSBleNativePlugin.OnCharacteristicValue += OnCharacteristicValueReceived;
                }

                // Call native plugin to write characteristic
                bool writeStarted = MacOSBleNativePlugin.WriteCharacteristic(_deviceAddress, _serviceUuid, _characteristicUuid, _data);

                if (!writeStarted)
                {
                    throw new InvalidOperationException($"Failed to start writing to characteristic {_characteristicUuid}");
                }

                if (_withResponse)
                {
                    // Wait for write confirmation with timeout
                    using var timeoutTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    timeoutTokenSource.CancelAfter(5000); // 5 second timeout

                    timeoutTokenSource.Token.Register(() => {
                        _completionSource?.TrySetCanceled();
                    });

                    await _completionSource.Task.ConfigureAwait(false);
                }
                else
                {
                    // For write without response, just wait a bit
                    await Task.Delay(100, cancellationToken);
                }

                Debug.Log($"[macOS BLE Command] Successfully wrote {_data.Length} bytes to characteristic {_characteristicUuid}");
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning($"[macOS BLE Command] Write operation timed out for characteristic {_characteristicUuid}");
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[macOS BLE Command] Error writing characteristic {_characteristicUuid}: {ex.Message}");
                throw;
            }
            finally
            {
                if (_withResponse)
                {
                    MacOSBleNativePlugin.OnCharacteristicValue -= OnCharacteristicValueReceived;
                }
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
                    // This is a write confirmation
                    _completionSource?.TrySetResult(true);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[macOS BLE Command] Error processing write confirmation callback: {ex.Message}");
                _completionSource?.TrySetException(ex);
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                if (_withResponse)
                {
                    MacOSBleNativePlugin.OnCharacteristicValue -= OnCharacteristicValueReceived;
                }
                _completionSource?.TrySetCanceled();
                _disposed = true;
            }
        }
    }
}