using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityBLE.windows
{
    public class WindowsDisconnectDeviceCommand : IDisposable
    {
        private readonly TaskCompletionSource<bool> _completionSource = new();
        private readonly IBlePeripheral _device;
        private bool _disposed;

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

        public async Task<bool> ExecuteAsync(IBlePeripheral device, CancellationToken cancellationToken = default)
        {
            if (device == null)
            {
                Debug.LogError(" Device cannot be null.");
                Dispose();
                return false;
            }

            if (string.IsNullOrEmpty(device.UUID))
            {
                Debug.LogError(" Device address is not set.");
                Dispose();
                return false;
            }

            if (!device.IsConnected)
            {
                Debug.LogWarning($" Device {device.UUID} is not connected.");
                Dispose();
                return false;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                Dispose();
                cancellationToken.ThrowIfCancellationRequested();
            }

            Debug.Log($" Disconnecting from device {device.UUID}...");

            WindowsBleNativePlugin.DisconnectFromDevice(device.UUID);

            try
            {
                using var cancellationRegistration = cancellationToken.Register(() => _completionSource.TrySetCanceled());
                return await _completionSource.Task;
            }
            finally
            {
                Dispose();
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            BleDeviceEvents.OnDisconnected -= OnDisconnected;
            _disposed = true;
        }
    }
}
