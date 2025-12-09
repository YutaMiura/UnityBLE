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

    }
}