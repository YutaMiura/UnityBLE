using System;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityBLE.windows
{
    internal class WindowsConnectDeviceCommand
    {
        private readonly TaskCompletionSource<IBlePeripheral> _connectionCompletionSource = new();
        private readonly WindowsBlePeripheral _targetDevice;

        public WindowsConnectDeviceCommand(WindowsBlePeripheral targetDevice)
        {
            _targetDevice = targetDevice;
        }

        public Task<IBlePeripheral> ExecuteAsync()
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
                WindowsBleNativePlugin.StopScan();
                Debug.Log($" Connecting to device {_targetDevice.UUID}...");
                BleDeviceEvents.OnConnected += OnDeviceConnected;

                // Start native connection
                WindowsBleNativePlugin.ConnectToDevice(_targetDevice.UUID);

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
            if (device.UUID != _targetDevice.UUID)
            {
                Debug.LogWarning($" Connected device UUID mismatch: expected {_targetDevice.UUID}, got {device.UUID}");
                return;
            }
            _connectionCompletionSource.TrySetResult(device);
            BleDeviceEvents.OnConnected -= OnDeviceConnected;
        }
    }
}
