using System;
using System.Threading.Tasks;

namespace UnityBLE
{
    public interface IBleScanner
    {
        Task InitializeAsync();

        Task StartScan(
            BleScanEventDelegates.DeviceDiscoveredDelegate OnDeviceDiscovered);
        Task StartScan(
            ScanFilter filter,
            BleScanEventDelegates.DeviceDiscoveredDelegate OnDeviceDiscovered);
        Task<bool> StopScan();

    }
}