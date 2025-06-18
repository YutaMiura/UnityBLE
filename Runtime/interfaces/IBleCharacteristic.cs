using System;
using System.Threading.Tasks;

namespace UnityBLE
{
    /// <summary>
    /// Interface representing a BLE characteristic.
    /// </summary>
    public interface IBleCharacteristic
    {
        string Uuid { get; }
        IBleService Service { get; }
        // Add additional properties as needed

        /// <summary>
        /// Read data from this characteristic.
        /// </summary>
        Task<byte[]> ReadAsync();

        /// <summary>
        /// Write data to this characteristic.
        /// </summary>
        Task WriteAsync(byte[] data, bool withResponse);

        /// <summary>
        /// Subscribe to notifications for this characteristic.
        /// </summary>
        Task SubscribeAsync(Action<byte[]> onValueChanged);

        /// <summary>
        /// Unsubscribe from notifications for this characteristic.
        /// </summary>
        Task UnsubscribeAsync();
    }
}