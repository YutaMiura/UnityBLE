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

        public Task StartScan(BleScanEventDelegates.DeviceDiscoveredDelegate OnDeviceDiscovered)
        {
            return StartScan(
                ScanFilter.None,
                OnDeviceDiscovered);
        }

        public Task StartScan(ScanFilter filter, BleScanEventDelegates.DeviceDiscoveredDelegate OnDeviceDiscovered)
        {
            if (_receiver != null)
            {
                _receiver.Stop();
                _receiver = null;
            }
            _receiver = new DeviceDiscoverReceiver(OnDeviceDiscovered);
            _receiver.Start();
            _scanCommand.Execute(filter);
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
            }
            return Task.FromResult(result);
        }

        private class DeviceDiscoverReceiver
        {
            BleScanEventDelegates.DeviceDiscoveredDelegate OnDeviceDiscovered;
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
        }
    }
}