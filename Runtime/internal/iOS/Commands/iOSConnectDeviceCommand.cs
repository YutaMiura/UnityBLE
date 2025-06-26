using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityBLE.iOS;

namespace UnityBLE.iOS
{
    internal class iOSConnectDeviceCommand
    {
        private readonly iOSNativeMessageParser _messageParser;
        private TaskCompletionSource<IBleDevice> _connectionCompletionSource;

        public iOSConnectDeviceCommand()
        {
            _messageParser = new iOSNativeMessageParser();

            // Subscribe to native plugin events
            iOSBleNativePlugin.OnDeviceConnected += OnDeviceConnected;
            iOSBleNativePlugin.OnError += OnConnectionError;
        }

        public async Task<IBleDevice> ExecuteAsync(string address, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(address))
            {
                throw new ArgumentException("Device address is not set.", nameof(address));
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Initialize native plugin if not already done
            if (!iOSBleNativePlugin.Initialize())
            {
                throw new InvalidOperationException("Failed to initialize iOS BLE native plugin");
            }

            _connectionCompletionSource = new TaskCompletionSource<IBleDevice>();

            try
            {
                Debug.Log($"[iOS BLE] Connecting to device {address}...");

                // Register cancellation callback
                cancellationToken.Register(() =>
                {
                    Debug.Log($"[iOS BLE] Connection to device {address} was cancelled.");
                    _connectionCompletionSource?.TrySetCanceled();
                });

                // Start native connection
                if (!iOSBleNativePlugin.ConnectToDevice(address))
                {
                    throw new InvalidOperationException($"Failed to start connection to device {address}");
                }

                // Wait for connection completion or cancellation
                var connectedDevice = await _connectionCompletionSource.Task;

                Debug.Log($"[iOS BLE] Successfully connected to device: {connectedDevice}");

                return connectedDevice;
            }
            catch (OperationCanceledException)
            {
                Debug.Log($"[iOS BLE] Connection to device {address} was cancelled.");
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
                Debug.Log($"[iOS BLE] Device connected callback: {device}");
                _connectionCompletionSource.TrySetResult(device);
            }
        }

        private void OnConnectionError(string errorMessage)
        {
            if (_connectionCompletionSource != null)
            {
                Debug.LogError($"[iOS BLE] Connection error: {errorMessage}");
                _connectionCompletionSource.TrySetException(new InvalidOperationException(errorMessage));
            }
        }

        ~iOSConnectDeviceCommand()
        {
            // Unsubscribe from events
            iOSBleNativePlugin.OnDeviceConnected -= OnDeviceConnected;
            iOSBleNativePlugin.OnError -= OnConnectionError;
        }
    }
}