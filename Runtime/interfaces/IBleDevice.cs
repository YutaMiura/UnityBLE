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

        /// <summary>
        /// Subscribe to essential device characteristics (Service Changed, etc.).
        /// This is typically called automatically after connection.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the subscription operation</param>
        Task SubscribeToEssentialCharacteristicsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Subscribe to a specific characteristic for notifications.
        /// </summary>
        /// <param name="serviceUuid">Service UUID</param>
        /// <param name="characteristicUuid">Characteristic UUID</param>
        /// <param name="cancellationToken">Token to cancel the subscription operation</param>
        Task SubscribeToCharacteristicAsync(string serviceUuid, string characteristicUuid, CancellationToken cancellationToken = default);

        /// <summary>
        /// Unsubscribe from a specific characteristic.
        /// </summary>
        /// <param name="serviceUuid">Service UUID</param>
        /// <param name="characteristicUuid">Characteristic UUID</param>
        /// <param name="cancellationToken">Token to cancel the unsubscription operation</param>
        Task UnsubscribeFromCharacteristicAsync(string serviceUuid, string characteristicUuid, CancellationToken cancellationToken = default);
    }
}