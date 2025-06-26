using System;
using System.Threading;
using UnityEngine;
using UnityBLE.iOS;

namespace UnityBLE.iOS
{
    public class iOSScanCommand
    {
        private readonly iOSNativeMessageParser _messageParser;
        private CancellationTokenSource _scanCancellationTokenSource;
        private bool _isScanning = false;

        private readonly BleScanEvents _events;

        public iOSScanCommand(BleScanEvents events)
        {
            _messageParser = new iOSNativeMessageParser();
            _events = events ?? throw new ArgumentNullException(nameof(events));

            // Subscribe to native plugin events
            iOSBleNativePlugin.OnDeviceDiscovered += OnDeviceDiscovered;
            iOSBleNativePlugin.OnScanCompleted += OnScanCompleted;
        }

        public void Execute(TimeSpan duration)
        {
            if (duration <= TimeSpan.Zero)
            {
                Debug.LogError("[iOS BLE] Scan duration must be greater than zero.");
                throw new ArgumentException("Scan duration must be greater than zero.");
            }

            if (_isScanning)
            {
                Debug.LogWarning("[iOS BLE] Scan is already in progress.");
                return;
            }

            // Initialize native plugin if not already done
            if (!iOSBleNativePlugin.Initialize())
            {
                throw new InvalidOperationException("Failed to initialize iOS BLE native plugin");
            }

            _isScanning = true;

            Debug.Log($"[iOS BLE] Starting native scan for {duration.TotalSeconds} seconds...");

            // Start native scan
            if (!iOSBleNativePlugin.StartScan(duration.TotalSeconds))
            {
                throw new StartScanFailed();
            }
            _isScanning = false;
            _scanCancellationTokenSource?.Dispose();
            _scanCancellationTokenSource = null;
        }

        public void Execute(TimeSpan duration, ScanFilter filter)
        {
            Debug.LogWarning("[iOS BLE] Scan filters are not yet implemented in iOS native plugin");
            Execute(duration);
        }

        private void OnDeviceDiscovered(string deviceJson)
        {
            var device = _messageParser.ParseDeviceData(deviceJson);
            if (device != null)
            {
                Debug.Log($"[iOS BLE] Discovered device: {device}");
                _events._deviceDiscovered?.Invoke(device);
            }
        }

        private void OnScanCompleted()
        {
            Debug.Log("[iOS BLE] Native scan completed");
            _events._scanCompleted?.Invoke();
        }

        ~iOSScanCommand()
        {
            // Unsubscribe from events
            iOSBleNativePlugin.OnDeviceDiscovered -= OnDeviceDiscovered;
            iOSBleNativePlugin.OnScanCompleted -= OnScanCompleted;
        }
    }
}