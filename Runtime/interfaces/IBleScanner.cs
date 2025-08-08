using System;
using System.Threading.Tasks;

namespace UnityBLE
{
    public interface IBleScanner
    {
        Task InitializeAsync();

        void StartScan(
            BleScanEventDelegates.DeviceDiscoveredDelegate OnDeviceDiscovered);
        void StartScan(
            ScanFilter filter,
            BleScanEventDelegates.DeviceDiscoveredDelegate OnDeviceDiscovered);
        bool StopScan();

    }
}