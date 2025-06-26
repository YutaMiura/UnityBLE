using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityBLE.macOS
{
    internal class MacOSConnectDeviceCommand
    {
        private readonly MacOSNativeMessageParser _messageParser;
        private TaskCompletionSource<IBleDevice> _connectionCompletionSource;

        public MacOSConnectDeviceCommand()
        {
            _messageParser = new MacOSNativeMessageParser();

            // Subscribe to native plugin events
            MacOSBleNativePlugin.OnDeviceConnected += OnDeviceConnected;
            MacOSBleNativePlugin.OnError += OnConnectionError;
        }

        ~MacOSConnectDeviceCommand()
        {
            // Unsubscribe from events to prevent memory leaks
            MacOSBleNativePlugin.OnDeviceConnected -= OnDeviceConnected;
            MacOSBleNativePlugin.OnError -= OnConnectionError;
        }

        public async Task<IBleDevice> ExecuteAsync(string address, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(address))
            {
                throw new ArgumentException("Device address is not set.", nameof(address));
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Initialize native plugin if not already done
            if (!MacOSBleNativePlugin.Initialize())
            {
                throw new InvalidOperationException("Failed to initialize macOS BLE native plugin");
            }

            _connectionCompletionSource = new TaskCompletionSource<IBleDevice>();

            try
            {
                Debug.Log($"[macOS BLE] Connecting to device {address}...");

                // Register cancellation callback
                cancellationToken.Register(() =>
                {
                    Debug.Log($"[macOS BLE] Connection to device {address} was cancelled.");
                    _connectionCompletionSource?.TrySetCanceled();
                });

                // Start native connection
                if (!MacOSBleNativePlugin.ConnectToDevice(address))
                {
                    throw new InvalidOperationException($"Failed to start connection to device {address}");
                }

                // Wait for connection completion or cancellation
                var connectedDevice = await _connectionCompletionSource.Task;

                Debug.Log($"[macOS BLE] Successfully connected to device: {connectedDevice}");

                return connectedDevice;
            }
            catch (OperationCanceledException)
            {
                Debug.Log($"[macOS BLE] Connection to device {address} was cancelled.");
                throw;
            }
            finally
            {
                _connectionCompletionSource = null;
            }
        }

        private void OnDeviceConnected(string deviceJson)
        {
            var device = _messageParser.ParseDeviceData(deviceJson);
            if (device != null && _connectionCompletionSource != null)
            {
                Debug.Log($"[macOS BLE] Device connected callback: {device}");
                _connectionCompletionSource.TrySetResult(device);
            }
        }

        private void OnConnectionError(string errorMessage)
        {
            if (_connectionCompletionSource != null)
            {
                Debug.LogError($"[macOS BLE] Connection error: {errorMessage}");
                _connectionCompletionSource.TrySetException(new InvalidOperationException(errorMessage));
            }
        }
    }
}