using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityBLE.windows
{
    /// <summary>
    /// Windows (C++/WinRT) implementation of <see cref="IBleScanner"/>.
    /// </summary>
    public sealed class WindowsBleScanner : IBleScanner
    {
        private readonly WindowsScanCommand _scanCommand;
        private readonly WindowsStopScanCommand _stopCommand;
        private readonly WindowsBleInitializeCommand _initializeCommand;

        private DeviceDiscoverReceiver _receiver;
        private CancellationTokenRegistration _ctsRegistration;

        public event Action<bool> OnScanningStateChanged;

        // Forwards the static BleScanEventDelegates.OnPeripheralUpdated to subscribers,
        // mirroring AndroidBleScanner / AppleBleScanner. Fires when an already-discovered
        // peripheral's advertisement payload changes (e.g. MSD arriving in a scan response).
        public event BleScanEventDelegates.PeripheralUpdatedDelegate OnPeripheralUpdated
        {
            add { BleScanEventDelegates.OnPeripheralUpdated += value; }
            remove { BleScanEventDelegates.OnPeripheralUpdated -= value; }
        }

        public bool IsInitialized => WindowsBleNativePlugin.IsInitialized;

        public WindowsBleScanner()
        {
            _scanCommand = new WindowsScanCommand();
            _initializeCommand = new WindowsBleInitializeCommand();
            _stopCommand = new WindowsStopScanCommand();
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
            _ctsRegistration.Dispose();
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
            if (!WindowsBleNativePlugin.IsScanning())
            {
                Debug.LogWarning("[UnityBLE] No scan in progress to stop.");
                if (_receiver != null)
                {
                    _receiver.Stop();
                    _receiver = null;
                }
                _ctsRegistration.Dispose();
                OnScanningStateChanged?.Invoke(false);
                return Task.FromResult(true);
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
            private readonly BleScanEventDelegates.DeviceDiscoveredDelegate OnDeviceDiscovered;

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
