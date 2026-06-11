using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityBLE.windows
{
    public class WindowsWriteCharacteristicCommand : IDisposable
    {
        private readonly string _deviceAddress;
        private readonly string _serviceUuid;
        private readonly string _characteristicUuid;
        private readonly byte[] _data;
        private readonly bool _withResponse;
        private TaskCompletionSource<bool> _completionSource;
        private bool _disposed;

        public WindowsWriteCharacteristicCommand(string deviceAddress, string serviceUuid, string characteristicUuid, byte[] data, bool withResponse)
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
                throw new ObjectDisposedException(nameof(WindowsWriteCharacteristicCommand));

            Debug.Log($"Writing {_data.Length} bytes to characteristic {_characteristicUuid} {(_withResponse ? "with" : "without")} response...");

            try
            {
                if (_withResponse)
                {
                    _completionSource = new TaskCompletionSource<bool>();
                    BleDeviceEvents.OnWriteOperationCompleted += OnWriteOperationCompleted;
                }

                // Call native plugin to write characteristic
                WindowsBleNativePlugin.WriteCharacteristic(_deviceAddress, _serviceUuid, _characteristicUuid, _data, _withResponse);

                if (_withResponse)
                {
                    using var timeoutTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    timeoutTokenSource.CancelAfter(5000); // 5 second timeout

                    timeoutTokenSource.Token.Register(() =>
                    {
                        _completionSource?.TrySetCanceled();
                    });

                    await _completionSource.Task.ConfigureAwait(false);
                }
                else
                {
                    await Task.Delay(100, cancellationToken);
                }

                Debug.Log($"Successfully wrote {_data.Length} bytes to characteristic {_characteristicUuid}");
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning($"Write operation timed out for characteristic {_characteristicUuid}");
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error writing characteristic {_characteristicUuid}: {ex.Message}");
                throw;
            }
            finally
            {
                if (_withResponse)
                {
                    BleDeviceEvents.OnWriteOperationCompleted -= OnWriteOperationCompleted;
                }
                _completionSource = null;
            }
        }

        private void OnWriteOperationCompleted(string characteristicUuid, bool success)
        {
            if (_characteristicUuid != characteristicUuid)
            {
                return; // Ignore events for other characteristics
            }
            _completionSource?.TrySetResult(success);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                if (_withResponse)
                {
                    BleDeviceEvents.OnWriteOperationCompleted -= OnWriteOperationCompleted;
                }
                _completionSource?.TrySetCanceled();
                _disposed = true;
            }
        }
    }
}
