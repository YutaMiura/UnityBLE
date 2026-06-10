using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityBLE.windows
{
    public class WindowsDisconnectDeviceCommand
    {
        private readonly TaskCompletionSource<bool> _completionSource = new TaskCompletionSource<bool>();
        private readonly IBlePeripheral _device;

        public WindowsDisconnectDeviceCommand(IBlePeripheral device)
        {
            _device = device ?? throw new ArgumentNullException(nameof(device), "Device cannot be null.");
            BleDeviceEvents.OnDisconnected += OnDisconnected;
        }

        private void OnDisconnected(string deviceUuid)
        {
            if (_device.UUID != deviceUuid)
            {
                return; // Ignore events for other devices
            }
            _completionSource.TrySetResult(true);
        }

        ~WindowsDisconnectDeviceCommand()
        {
            BleDeviceEvents.OnDisconnected -= OnDisconnected;
        }

        public Task<bool> ExecuteAsync(IBlePeripheral device, CancellationToken cancellationToken = default)
        {
            if (device == null)
            {
                Debug.LogError(" Device cannot be null.");
                return Task.FromResult(false);
            }

            if (string.IsNullOrEmpty(device.UUID))
            {
                Debug.LogError(" Device address is not set.");
                return Task.FromResult(false);
            }

            if (!device.IsConnected)
            {
                Debug.LogWarning($" Device {device.UUID} is not connected.");
                return Task.FromResult(false);
            }

            cancellationToken.ThrowIfCancellationRequested();

            Debug.Log($" Disconnecting from device {device.UUID}...");

            WindowsBleNativePlugin.DisconnectFromDevice(device.UUID);

            return _completionSource.Task;
        }
    }
}
