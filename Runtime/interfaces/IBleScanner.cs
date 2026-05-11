using System;
using System.Threading;
using System.Threading.Tasks;

namespace UnityBLE
{
    public interface IBleScanner
    {
        Task InitializeAsync();
        Task StartScan(
            BleScanEventDelegates.DeviceDiscoveredDelegate OnDeviceDiscovered,
            CancellationToken cancellationToken);
        Task StartScan(
            ScanFilter filter,
            BleScanEventDelegates.DeviceDiscoveredDelegate OnDeviceDiscovered,
            CancellationToken cancellationToken);
        Task<bool> StopScan();

        bool IsInitialized { get; }

        event Action<bool> OnScanningStateChanged;

        // Fired when an already-discovered peripheral's advertisement payload
        // changes (typically when MSD arrives in a SCAN_RSP after the initial
        // ADV_IND). The IBlePeripheral instance is the same reference as the
        // one delivered to OnDeviceDiscovered; its mutable properties are
        // updated in place.
        event BleScanEventDelegates.PeripheralUpdatedDelegate OnPeripheralUpdated;

    }
}