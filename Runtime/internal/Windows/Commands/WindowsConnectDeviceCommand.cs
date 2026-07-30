using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityBLE.windows
{
    internal class WindowsConnectDeviceCommand
    {
        private readonly TaskCompletionSource<IBlePeripheral> _connectionCompletionSource = new();
        private readonly WindowsBlePeripheral _targetDevice;
        private CancellationTokenRegistration _cancellationRegistration;

        public WindowsConnectDeviceCommand(WindowsBlePeripheral targetDevice)
        {
            _targetDevice = targetDevice;
        }

        public async Task<IBlePeripheral> ExecuteAsync(CancellationToken cancellationToken)
        {
            if (_targetDevice == null)
            {
                throw new ArgumentException("Target device is not set.", nameof(_targetDevice));
            }

            if (!WindowsBleNativePlugin.IsInitialized)
            {
                throw new InvalidOperationException("Failed to initialize Windows BLE native plugin");
            }

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Starting GATT while the advertisement watcher is still tearing
                // down makes the first service query return Unreachable. The
                // native StopScanning blocks until the watcher reports Stopped,
                // so run it off the main thread rather than stalling a frame.
                await Task.Run(WindowsBleNativePlugin.StopScan, cancellationToken);

                Debug.Log($" Connecting to device {_targetDevice.UUID}...");
                BleDeviceEvents.OnConnected += OnDeviceConnected;
                BleDeviceEvents.OnDisconnected += OnDeviceDisconnected;
                _cancellationRegistration = cancellationToken.Register(OnConnectionCancelled);

                // Start native connection
                WindowsBleNativePlugin.ConnectToDevice(_targetDevice.UUID);

                // Wait for connection completion or cancellation
                return await _connectionCompletionSource.Task;
            }
            catch (OperationCanceledException)
            {
                Debug.Log($" Connection to device {_targetDevice.UUID} was cancelled.");
                Cleanup();
                throw;
            }
            catch
            {
                Cleanup();
                throw;
            }
        }

        private void OnDeviceConnected(IBlePeripheral device)
        {
            Debug.Log($" Device connected: {_targetDevice.UUID}");
            if (device.UUID != _targetDevice.UUID)
            {
                Debug.LogWarning($" Connected device UUID mismatch: expected {_targetDevice.UUID}, got {device.UUID}");
                return;
            }
            _connectionCompletionSource.TrySetResult(device);
            Cleanup();
        }

        private void OnDeviceDisconnected(string deviceUuid)
        {
            if (deviceUuid != _targetDevice.UUID) return;

            Cleanup();
            _connectionCompletionSource.TrySetException(
                new InvalidOperationException(
                    $"Windows could not establish a GATT connection to {_targetDevice.UUID}."));
        }

        private void OnConnectionCancelled()
        {
            Debug.Log($" Connection to device {_targetDevice.UUID} was cancelled.");
            Cleanup();

            try
            {
                WindowsBleNativePlugin.DisconnectFromDevice(_targetDevice.UUID);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($" Failed to disconnect cancelled device {_targetDevice.UUID}: {ex.Message}");
            }

            _connectionCompletionSource.TrySetCanceled();
        }

        private void Cleanup()
        {
            BleDeviceEvents.OnConnected -= OnDeviceConnected;
            BleDeviceEvents.OnDisconnected -= OnDeviceDisconnected;
            _cancellationRegistration.Dispose();
        }
    }
}
