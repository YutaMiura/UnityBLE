using System;
using System.Threading;
using UnityEngine;

namespace UnityBLE.macOS
{
    public class MacOSScanCommand
    {
        private readonly MacOSNativeMessageParser _messageParser;
        private CancellationTokenSource _scanCancellationTokenSource;
        private bool _isScanning = false;

        private readonly BleScanEvents _events;

        public MacOSScanCommand(BleScanEvents events)
        {
            _messageParser = new MacOSNativeMessageParser();
            _events = events ?? throw new ArgumentNullException(nameof(events));

            // Subscribe to native plugin events
            MacOSBleNativePlugin.OnDeviceDiscovered += OnDeviceDiscovered;
            MacOSBleNativePlugin.OnScanCompleted += OnScanCompleted;
        }

        public void Execute(TimeSpan duration, ScanFilter filter = null)
        {
            if (duration <= TimeSpan.Zero)
            {
                Debug.LogError("[macOS BLE] Scan duration must be greater than zero.");
                throw new ArgumentException("Scan duration must be greater than zero.");
            }

            if (_isScanning)
            {
                Debug.LogWarning("[macOS BLE] Scan is already in progress.");
                return;
            }

            // Initialize native plugin if not already done
            if (!MacOSBleNativePlugin.Initialize())
            {
                throw new InvalidOperationException("Failed to initialize macOS BLE native plugin");
            }

            _isScanning = true;

            Debug.Log($"[macOS BLE] Starting native scan for {duration.TotalSeconds} seconds...");

            // Start native scan
            if (!MacOSBleNativePlugin.StartScan(duration.TotalSeconds))
            {
                _isScanning = false;
                throw new StartScanFailed();
            }
            _scanCancellationTokenSource?.Dispose();
            _scanCancellationTokenSource = null;
        }

        private void OnDeviceDiscovered(string deviceJson)
        {
            var device = _messageParser.ParseDeviceData(deviceJson);
            if (device != null)
            {
                Debug.Log($"[macOS BLE] Discovered device: {device}");
                _events._deviceDiscovered?.Invoke(device);
            }
        }

        private void OnScanCompleted()
        {
            Debug.Log("[macOS BLE] Native scan completed");
            _isScanning = false;
            _events._scanCompleted?.Invoke();
        }

        ~MacOSScanCommand()
        {
            // Unsubscribe from events
            MacOSBleNativePlugin.OnDeviceDiscovered -= OnDeviceDiscovered;
            MacOSBleNativePlugin.OnScanCompleted -= OnScanCompleted;
        }
    }
}