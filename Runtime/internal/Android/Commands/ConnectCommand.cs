using System;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityBLE.Android
{
    public class ConnectCommand
    {
        private readonly TaskCompletionSource<IBlePeripheral> _connectionCompletionSource = new();
        private readonly AndroidBlePeripheral _targetDevice;
        private readonly AndroidBleNativePlugin _plugin;

        internal ConnectCommand(AndroidBlePeripheral targetDevice, AndroidBleNativePlugin plugin)
        {
            _plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
            _targetDevice = targetDevice;
        }

        public async Task<IBlePeripheral> ExecuteAsync()
        {
            if (_targetDevice == null)
            {
                throw new ArgumentException("Target device is not set.", nameof(_targetDevice));
            }

            await NativeFacade.Instance.StopScanAsync();

            // Register event handler for connection
            BleDeviceEvents.OnConnected += OnDeviceConnected;
            // Start native connection
            _plugin.Connect(_targetDevice);
            return await _connectionCompletionSource.Task;
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