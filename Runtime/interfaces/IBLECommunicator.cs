using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.LightTransport;

namespace UnityBLE
{
    /// <summary>
    /// Abstract BLE client interface for Unity BLE communication. Responsible for initialization and device scanning.
    /// </summary>
    ///
    public interface IBLECommunicator
    {
        /// <summary>
        /// Initialize the BLE client (including permission checks if necessary).
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the initialization operation</param>
        Task InitializeAsync(BleScanEvents events, CancellationToken cancellationToken = default);

        /// <summary>
        /// Start scanning for BLE devices for a specified duration.
        /// Devices will be reported via the DeviceDiscovered event.
        /// </summary>
        /// <param name="duration">Duration to scan for BLE devices</param>
        void StartScan(TimeSpan duration);

        /// <summary>
        /// Start scanning for BLE devices advertising any of the specified service UUIDs for a specified duration.
        /// Devices will be reported via the DeviceDiscovered event.
        /// </summary>
        /// <param name="duration">Duration to scan for BLE devices</param>
        /// <param name="serviceUuids">Service UUIDs to filter by</param>
        void StartScanByServiceUuids(TimeSpan duration, params string[] serviceUuids);

        /// <summary>
        /// Start scanning for BLE devices with a name filter for a specified duration.
        /// Devices will be reported via the DeviceDiscovered event.
        /// </summary>
        /// <param name="duration">Duration to scan for BLE devices</param>
        /// <param name="names">Device names to filter by</param>
        void StartScanByNames(TimeSpan duration, params string[] names);

        /// <summary>
        /// Stop the BLE scan.
        /// </summary>
        void StopScan();
    }
}