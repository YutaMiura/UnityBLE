using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityBLE.macOS
{
    internal class MacOSConnectDeviceCommand
    {
        private readonly MacOSNativeMessageParser _messageParser;

        private readonly TaskCompletionSource<bool> _connectionCompletionSource = new();
        private MacOSBleDevice _targetDevice;

        public MacOSConnectDeviceCommand(MacOSBleDevice targetDevice)
        {
            _messageParser = new MacOSNativeMessageParser();
            _targetDevice = targetDevice;

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

        public async Task<IBleDevice> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            if (_targetDevice == null)
            {
                throw new ArgumentException("Target device is not set.", nameof(_targetDevice));
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Initialize native plugin if not already done
            if (!MacOSBleNativePlugin.Initialize())
            {
                throw new InvalidOperationException("Failed to initialize macOS BLE native plugin");
            }

            try
            {
                Debug.Log($"[macOS BLE] Connecting to device {_targetDevice.Address}...");

                // Register cancellation callback
                cancellationToken.Register(() =>
                {
                    Debug.Log($"[macOS BLE] Connection to device {_targetDevice.Address} was cancelled.");
                    _connectionCompletionSource.TrySetCanceled();
                });

                // Start native connection
                if (!MacOSBleNativePlugin.ConnectToDevice(_targetDevice.Address))
                {
                    throw new InvalidOperationException($"Failed to start connection to device {_targetDevice.Address}");
                }

                // Wait for connection completion or cancellation
                if (await _connectionCompletionSource.Task)
                {
                    Debug.Log($"[macOS BLE] Successfully connected to device: {_targetDevice}");
                    return _targetDevice;
                }
                else
                {
                    throw new InvalidOperationException($"Connection to device {_targetDevice.Address} failed");
                }
            }
            catch (OperationCanceledException)
            {
                Debug.Log($"[macOS BLE] Connection to device {_targetDevice.Address} was cancelled.");
                throw;
            }
        }

        private void OnDeviceConnected(string deviceJson)
        {
            var device = _messageParser.ParseDeviceData(deviceJson);
            _targetDevice = device as MacOSBleDevice;
            Debug.Log($"[macOS BLE] Device connected: {device.Address}");
            _connectionCompletionSource.TrySetResult(true);
        }

        private void OnConnectionError(string errorMessage)
        {
            _connectionCompletionSource.TrySetException(new InvalidOperationException(errorMessage));
        }
    }
}