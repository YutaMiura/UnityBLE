using System;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityBLE.Android
{
    public sealed class AndroidBleScanner : IBleScanner
    {
        private NativeFacade _facade;

        public Task InitializeAsync()
        {
            _facade = NativeFacade.Instance;
            return Task.CompletedTask;
        }

        Task IBleScanner.StartScan(BleScanEventDelegates.DeviceDiscoveredDelegate OnDeviceDiscovered)
        {
            if (_facade == null)
            {
                Debug.LogError("AndroidBleScanner is not initialized.");
                return Task.FromException(new InvalidOperationException("AndroidBleScanner is not initialized."));
            }
            Debug.Log("AndroidBleScanner.StartScan() called with no filter.");
            return _facade.StartScan(
                ScanFilter.None,
                OnDeviceDiscovered);
        }

        Task IBleScanner.StartScan(ScanFilter filter, BleScanEventDelegates.DeviceDiscoveredDelegate OnDeviceDiscovered)
        {
            if (_facade == null)
            {
                Debug.LogError("AndroidBleScanner is not initialized.");
                return Task.FromException(new InvalidOperationException("AndroidBleScanner is not initialized."));
            }
            Debug.Log($"AndroidBleScanner.StartScan() called with filter: {filter ?? ScanFilter.None}");
            return _facade.StartScan(
                filter,
                OnDeviceDiscovered);
        }

        Task<bool> IBleScanner.StopScan()
        {
            if (_facade == null)
            {
                Debug.LogError("AndroidBleScanner is not initialized.");
                return Task.FromException<bool>(new InvalidOperationException("AndroidBleScanner is not initialized."));
            }
            Debug.Log("AndroidBleScanner.StopScan() called.");
            return _facade.StopScanAsync();
        }
    }
}