using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityBLE.Android
{
    public sealed class AndroidBleScanner : IBleScanner
    {
        private NativeFacade _facade;

        private CancellationTokenRegistration _ctsRegistration;

        public Task InitializeAsync()
        {
            _facade = NativeFacade.Instance;
            return Task.CompletedTask;
        }

        public Task StartScan(BleScanEventDelegates.DeviceDiscoveredDelegate OnDeviceDiscovered, CancellationToken cancellationToken)
        {
            return StartScan(ScanFilter.None, OnDeviceDiscovered, cancellationToken);
        }

        public Task StartScan(ScanFilter filter, BleScanEventDelegates.DeviceDiscoveredDelegate OnDeviceDiscovered, CancellationToken cancellationToken)
        {
            if (_facade == null)
            {
                Debug.LogError("AndroidBleScanner is not initialized.");
                return Task.FromException(new InvalidOperationException("AndroidBleScanner is not initialized."));
            }
            Debug.Log($"AndroidBleScanner.StartScan() called with filter: {filter ?? ScanFilter.None}");
            _ctsRegistration = cancellationToken.Register(() =>
            {
                StopScan().ConfigureAwait(false);
            });
            return _facade.StartScan(
                filter,
                OnDeviceDiscovered);
        }

        public async Task<bool> StopScan()
        {
            if (_facade == null)
            {
                Debug.LogError("AndroidBleScanner is not initialized.");
                throw new InvalidOperationException("AndroidBleScanner is not initialized.");
            }

            Debug.Log("AndroidBleScanner.StopScan() called.");
            var result = await _facade.StopScanAsync();
            _ctsRegistration.Dispose();
            return result;
        }
    }
}