using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UnityBLE
{
    /// <summary>
    /// Abstract BLE client interface for Unity BLE communication. Responsible for initialization and device scanning.
    /// </summary>
    public interface IBLECommunicator
    {
        /// <summary>
        /// Initialize the BLE client (including permission checks if necessary).
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Scan for BLE devices for a specified duration.
        /// </summary>
        Task<IReadOnlyList<IBleDevice>> ScanAsync(TimeSpan duration);

        /// <summary>
        /// Scan for BLE devices advertising any of the specified service UUIDs for a specified duration.
        /// </summary>
        Task<IReadOnlyList<IBleDevice>> ScanByServiceUuidsAsync(TimeSpan duration, params string[] serviceUuids);

        /// <summary>
        /// Scan for BLE devices with a name filter for a specified duration.
        /// </summary>
        Task<IReadOnlyList<IBleDevice>> ScanByNamesAsync(TimeSpan duration, params string[] names);

        /// <summary>
        /// Stop the BLE scan.
        /// </summary>
        void StopScan();

        /// <summary>
        /// Event triggered when a BLE device is discovered.
        /// </summary>
        event Action<IBleDevice> DeviceDiscovered;
    }
}