using System;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityBLE.apple
{
    internal class AppleConnectDeviceCommand
    {
        private readonly TaskCompletionSource<IBlePeripheral> _connectionCompletionSource = new();
        private AppleBlePeripheral _targetDevice;

        public AppleConnectDeviceCommand(AppleBlePeripheral targetDevice)
        {
            _targetDevice = targetDevice;
        }

        public Task<IBlePeripheral> ExecuteAsync()
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
                AppleBleNativePlugin.StopScan();
                Debug.Log($" Connecting to device {_targetDevice.UUID}...");

                // Start native connection
                AppleBleNativePlugin.ConnectToDevice(_targetDevice.UUID);

                // Wait for connection completion or cancellation
                return _connectionCompletionSource.Task;

            }
            catch (OperationCanceledException)
            {
                Debug.Log($" Connection to device {_targetDevice.UUID} was cancelled.");
                throw;
            }
        }

        private void OnDeviceConnected(IBlePeripheral device)
        {
            Debug.Log($" Device connected: {_targetDevice.UUID}");
            _connectionCompletionSource.TrySetResult(device);
        }
    }
}