using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityBLE.apple
{
    internal class AppleConnectDeviceCommand
    {
        private readonly TaskCompletionSource<IBlePeripheral> _connectionCompletionSource = new();
        private AppleBlePeripheral _targetDevice;
        private CancellationTokenRegistration _cancellationRegistration;

        public AppleConnectDeviceCommand(AppleBlePeripheral targetDevice)
        {
            _targetDevice = targetDevice;
        }

        public Task<IBlePeripheral> ExecuteAsync(CancellationToken cancellationToken)
        {
            if (_targetDevice == null)
            {
                throw new ArgumentException("Target device is not set.", nameof(_targetDevice));
            }

            // Initialize native plugin if not already done
            if (!AppleBleNativePlugin.IsInitialized)
            {
                throw new InvalidOperationException("Failed to initialize macOS BLE native plugin");
            }

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                AppleBleNativePlugin.StopScan();
                Debug.Log($" Connecting to device {_targetDevice.UUID}...");
                BleDeviceEvents.OnConnected += OnDeviceConnected;

                // CoreBluetooth's connectPeripheral has no timeout and keeps a pending connection
                // alive indefinitely, so honor cancellation (the caller's token, or the library
                // connect timeout linked into it): tear down the native connect and complete as
                // cancelled instead of leaving the await hanging.
                _cancellationRegistration = cancellationToken.Register(OnConnectionCancelled);

                // Start native connection
                AppleBleNativePlugin.ConnectToDevice(_targetDevice.UUID);

                // Wait for connection completion or cancellation
                return _connectionCompletionSource.Task;
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

        private void OnConnectionCancelled()
        {
            Debug.Log($" Connection to device {_targetDevice.UUID} was cancelled.");
            Cleanup();

            try
            {
                AppleBleNativePlugin.DisconnectFromDevice(_targetDevice.UUID);
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
            _cancellationRegistration.Dispose();
        }
    }
}
