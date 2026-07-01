using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityBLE.Android
{
    public class ConnectCommand
    {
        // GATT_ERROR (status 133) / connection-failed-to-establish is the most common Android
        // BLE failure and is transient: the native onConnectionStateChange reports it as
        // STATE_DISCONNECTED before the link ever reaches STATE_CONNECTED. A single attempt
        // therefore aborts on what is usually a recoverable hiccup, so we retry a bounded
        // number of times. The native side already closes the BluetoothGatt on every
        // STATE_DISCONNECTED (including failed connects), so each retry is a clean fresh
        // connectGatt - no explicit close needed here.
        private const int DefaultMaxAttempts = 3;
        private const int DefaultRetryDelayMs = 300;

        private readonly AndroidBlePeripheral _targetDevice;
        private readonly AndroidBleNativePlugin _plugin;
        private readonly int _maxAttempts;
        private readonly int _retryDelayMs;

        // Completion source for the CURRENT attempt only. Recreated per attempt so a late
        // disconnect from a previous attempt (delivered while unsubscribed) cannot fail a
        // later one.
        private TaskCompletionSource<IBlePeripheral> _attempt;

        internal ConnectCommand(
            AndroidBlePeripheral targetDevice,
            AndroidBleNativePlugin plugin,
            int maxAttempts = DefaultMaxAttempts,
            int retryDelayMs = DefaultRetryDelayMs)
        {
            _plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
            _targetDevice = targetDevice;
            _maxAttempts = Math.Max(1, maxAttempts);
            _retryDelayMs = Math.Max(0, retryDelayMs);
        }

        public async Task<IBlePeripheral> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            if (_targetDevice == null)
            {
                throw new ArgumentException("Target device is not set.", nameof(_targetDevice));
            }

            await NativeFacade.Instance.StopScanAsync();

            for (var attempt = 1; attempt <= _maxAttempts; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    return await AttemptConnectAsync(cancellationToken);
                }
                catch (ConnectFailedException)
                {
                    // A transient connect failure (e.g. GATT 133 / disconnected during connect).
                    // Retry unless this was the last attempt.
                    if (attempt >= _maxAttempts)
                    {
                        Debug.LogError(
                            $"[UnityBLE] Failed to connect to device {_targetDevice.UUID} after {_maxAttempts} attempts (disconnected during connect).");
                        throw new InvalidOperationException(
                            $"Failed to connect to device {_targetDevice.UUID} (disconnected during connect).");
                    }

                    Debug.LogWarning(
                        $"[UnityBLE] Connect attempt {attempt}/{_maxAttempts} for {_targetDevice.UUID} failed (disconnected during connect); retrying in {_retryDelayMs}ms.");
                    await Task.Delay(_retryDelayMs, cancellationToken);
                }
            }

            // Unreachable: the loop either returns on success or throws on the final attempt.
            throw new InvalidOperationException(
                $"Failed to connect to device {_targetDevice.UUID} (disconnected during connect).");
        }

        // Runs exactly one connectGatt attempt. Resolves on a matching connect, and completes
        // as a retryable failure on a matching disconnect (a connect that never established) or
        // as cancellation. A board that never responds produces no callback at all, so the
        // caller's cancellationToken is the only bound on a silent attempt.
        private async Task<IBlePeripheral> AttemptConnectAsync(CancellationToken cancellationToken)
        {
            _attempt = new TaskCompletionSource<IBlePeripheral>();

            BleDeviceEvents.OnConnected += OnDeviceConnected;
            BleDeviceEvents.OnDisconnected += OnDeviceDisconnected;

            using var registration = cancellationToken.Register(
                () => _attempt.TrySetCanceled(cancellationToken));

            try
            {
                _plugin.Connect(_targetDevice);
                return await _attempt.Task;
            }
            finally
            {
                // Unsubscribe before the inter-attempt delay so a stale disconnect for this
                // device is ignored rather than failing the next attempt.
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
            _attempt?.TrySetResult(device);
        }

        private void OnDeviceDisconnected(string deviceUuid)
        {
            if (deviceUuid != _targetDevice.UUID)
            {
                return;
            }
            // A disconnect arriving before the connection completes means the connect attempt failed
            // (the native onConnectionStateChange reports failed connects as STATE_DISCONNECTED).
            // Surface it as a retryable failure instead of letting the await hang.
            _attempt?.TrySetException(new ConnectFailedException());
        }

        // Internal signal that an attempt failed with a disconnect-during-connect and may be
        // retried. Never leaves ExecuteAsync (mapped to InvalidOperationException on final failure).
        private sealed class ConnectFailedException : Exception
        {
        }
    }
}
