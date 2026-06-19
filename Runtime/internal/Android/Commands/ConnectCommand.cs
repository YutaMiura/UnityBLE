using System;
using System.Threading;
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

        public async Task<IBlePeripheral> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            if (_targetDevice == null)
            {
                throw new ArgumentException("Target device is not set.", nameof(_targetDevice));
            }

            await NativeFacade.Instance.StopScanAsync();

            // Resolve on success, but also fail on a disconnect and on cancellation/timeout. A failed
            // connect (e.g. GATT status 133) is reported by the native layer as STATE_DISCONNECTED, and
            // a board that never responds produces no callback at all. Without handling these, the
            // await would hang forever (the native connect has no timeout of its own).
            BleDeviceEvents.OnConnected += OnDeviceConnected;
            BleDeviceEvents.OnDisconnected += OnDeviceDisconnected;

            using var registration = cancellationToken.Register(
                () => _connectionCompletionSource.TrySetCanceled(cancellationToken));

            try
            {
                _plugin.Connect(_targetDevice);
                return await _connectionCompletionSource.Task;
            }
            finally
            {
                BleDeviceEvents.OnConnected -= OnDeviceConnected;
                BleDeviceEvents.OnDisconnected -= OnDeviceDisconnected;
            }
        }

        private void OnDeviceConnected(IBlePeripheral device)
        {
            if (device.UUID != _targetDevice.UUID)
            {
                Debug.LogWarning($" Connected device UUID mismatch: expected {_targetDevice.UUID}, got {device.UUID}");
                return;
            }
            Debug.Log($" Device connected: {_targetDevice.UUID}");
            _connectionCompletionSource.TrySetResult(device);
        }

        private void OnDeviceDisconnected(string deviceUuid)
        {
            if (deviceUuid != _targetDevice.UUID)
            {
                return;
            }
            // A disconnect arriving before the connection completes means the connect attempt failed
            // (the native onConnectionStateChange reports failed connects as STATE_DISCONNECTED).
            // Surface it as a failure instead of letting the await hang.
            Debug.LogWarning($" Connection to {_targetDevice.UUID} failed (disconnected during connect).");
            _connectionCompletionSource.TrySetException(
                new InvalidOperationException($"Failed to connect to device {_targetDevice.UUID} (disconnected during connect)."));
        }
    }
}
