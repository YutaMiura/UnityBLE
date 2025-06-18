using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UnityBLE
{
    /// <summary>
    /// Interface representing a BLE device.
    /// </summary>
    public interface IBleDevice
    {
        string Name { get; }
        string Address { get; }

        /// <summary>
        /// Connect to this BLE device.
        /// </summary>
        Task ConnectAsync();

        /// <summary>
        /// Disconnect from this BLE device.
        /// </summary>
        Task DisconnectAsync();

        /// <summary>
        /// Get the list of services from this device.
        /// </summary>
        Task<IReadOnlyList<IBleService>> GetServicesAsync();
    }
}