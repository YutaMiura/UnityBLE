using System.Collections.Generic;
using System.Threading;
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

        public bool IsConnected { get; }

        public IEnumerable<IBleService> Services { get; }

        public BleDeviceEvents Events { get; }

        /// <summary>
        /// Connect to this BLE device.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the connection operation</param>
        Task ConnectAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Disconnect from this BLE device.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the disconnection operation</param>
        Task DisconnectAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Get the array of services from this device.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the service discovery operation</param>
        Task ReloadServicesAsync(CancellationToken cancellationToken = default);
    }
}