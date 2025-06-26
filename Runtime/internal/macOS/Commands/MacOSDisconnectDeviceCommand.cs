using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityBLE.macOS
{
    public class MacOSDisconnectDeviceCommand
    {
        private readonly TaskCompletionSource<bool> _completionSource = new TaskCompletionSource<bool>();
        public MacOSDisconnectDeviceCommand()
        {
            MacOSBleNativePlugin.OnDeviceDisconnected += OnDisconnected;
        }

        private void OnDisconnected(string deviceJson)
        {
            _completionSource.TrySetResult(true);
        }

        ~MacOSDisconnectDeviceCommand()
        {
            MacOSBleNativePlugin.OnDeviceDisconnected -= OnDisconnected;
        }

        public Task<bool> ExecuteAsync(IBleDevice device, CancellationToken cancellationToken = default)
        {
            if (device == null)
            {
                Debug.LogError("[macOS BLE] Device cannot be null.");
                return Task.FromResult(false);
            }

            if (string.IsNullOrEmpty(device.Address))
            {
                Debug.LogError("[macOS BLE] Device address is not set.");
                return Task.FromResult(false);
            }

            if (!device.IsConnected)
            {
                Debug.LogWarning($"[macOS BLE] Device {device.Address} is not connected.");
                return Task.FromResult(false);
            }

            cancellationToken.ThrowIfCancellationRequested();

            Debug.Log($"[macOS BLE] Disconnecting from device {device.Address}...");

            MacOSBleNativePlugin.DisconnectFromDevice(device.Address);

            return _completionSource.Task;
        }
    }
}