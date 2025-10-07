using System;
using System.Threading;
using System.Threading.Tasks;
using UnityBle.macOS;
using UnityEngine;

namespace UnityBLE.apple
{
    public sealed class AppleBleScanner : IBleScanner
    {
        private readonly AppleScanCommand _scanCommand;
        private readonly AppleStopScanCommand _stopCommand;

        private readonly AppleBleInitializeCommand _initializeCommand;

        DeviceDiscoverReceiver _receiver;

        private CancellationTokenRegistration _ctsRegistration;

        public event Action<bool> OnScanningStateChanged;

        public AppleBleScanner()
        {
            _scanCommand = new AppleScanCommand();
            _initializeCommand = new AppleBleInitializeCommand();
            _stopCommand = new AppleStopScanCommand();
            BleScanEventDelegates.BLEStatusChanged += OnBLEStatusChanged;
        }

        private void OnBLEStatusChanged(BleStatus status)
        {
            Debug.Log($"BLE Status Changed: {status}");
        }

        public Task InitializeAsync()
        {
            return _initializeCommand.ExecuteAsync();
        }

        public Task StartScan(BleScanEventDelegates.DeviceDiscoveredDelegate OnDeviceDiscovered, CancellationToken cancellationToken)
        {
            return StartScan(
                ScanFilter.None,
                OnDeviceDiscovered,
                cancellationToken);
        }

        public Task StartScan(ScanFilter filter, BleScanEventDelegates.DeviceDiscoveredDelegate OnDeviceDiscovered, CancellationToken cancellationToken)
        {
            if (_receiver != null)
            {
                _receiver.Stop();
                _receiver = null;
            }
            _receiver = new DeviceDiscoverReceiver(OnDeviceDiscovered);
            _receiver.Start();
            _ctsRegistration = cancellationToken.Register(() =>
            {
                StopScan();
            });
            _scanCommand.Execute(filter);
            OnScanningStateChanged?.Invoke(true);
            return Task.CompletedTask;
        }

        public Task<bool> StopScan()
        {
            if (!AppleBleNativePlugin.IsScanning())
            {
                Debug.LogWarning("[UnityBLE] No scan in progress to stop.");
                return Task.FromResult(false);
            }
            var result = _stopCommand.Execute();
            if (result)
            {
                if (_receiver != null)
                {
                    _receiver.Stop();
                    _receiver = null;
                }
                _ctsRegistration.Dispose();
                OnScanningStateChanged?.Invoke(false);
            }
            return Task.FromResult(result);
        }

        private class DeviceDiscoverReceiver
        {
            readonly BleScanEventDelegates.DeviceDiscoveredDelegate OnDeviceDiscovered;
            public DeviceDiscoverReceiver(BleScanEventDelegates.DeviceDiscoveredDelegate onDeviceDiscovered)
            {
                OnDeviceDiscovered = onDeviceDiscovered;
            }
            public void Start()
            {
                BleScanEventDelegates.OnDeviceDiscovered += OnDeviceDiscovered;
            }

            public void Stop()
            {
                BleScanEventDelegates.OnDeviceDiscovered -= OnDeviceDiscovered;
            }

            private void OnDeviceDiscoveredHandler(IBlePeripheral device)
            {
                OnDeviceDiscovered?.Invoke(device);
            }
        }
    }
}