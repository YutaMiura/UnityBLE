using System;
using System.Threading;
using System.Threading.Tasks;

namespace UnityBLE
{
    /// <summary>
    /// Interface representing a BLE characteristic.
    /// </summary>
    public interface IBleCharacteristic
    {
        string Uuid { get; }

        /// <summary>
        /// Read data from this characteristic.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the read operation</param>
        Task<byte[]> ReadAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Write data to this characteristic.
        /// </summary>
        /// <param name="data">Data to write</param>
        /// <param name="withResponse">Whether to wait for a response</param>
        /// <param name="cancellationToken">Token to cancel the write operation</param>
        Task WriteAsync(byte[] data, bool withResponse, CancellationToken cancellationToken = default);

        /// <summary>
        /// Subscribe to notifications for this characteristic.
        /// </summary>
        /// <param name="onValueChanged">Callback when value changes</param>
        /// <param name="cancellationToken">Token to cancel the subscription operation</param>
        Task SubscribeAsync(Action<byte[]> onValueChanged, CancellationToken cancellationToken = default);

        /// <summary>
        /// Unsubscribe from notifications for this characteristic.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the unsubscription operation</param>
        Task UnsubscribeAsync(CancellationToken cancellationToken = default);
    }
}