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
        CharacteristicProperties Properties { get; }
        
        /// <summary>
        /// Check if the characteristic supports reading
        /// </summary>
        bool CanRead => Properties.CanRead();
        
        /// <summary>
        /// Check if the characteristic supports writing
        /// </summary>
        bool CanWrite => Properties.CanWrite();
        
        /// <summary>
        /// Check if the characteristic supports notifications
        /// </summary>
        bool CanNotify => Properties.CanNotify();

        public event Action<byte[]> OnDataReceived;

        /// <summary>
        /// Read data from this characteristic.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the read operation</param>
        Task<byte[]> ReadAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Write data to this characteristic.
        /// </summary>
        /// <param name="data">Data to write</param>
        /// <param name="cancellationToken">Token to cancel the write operation</param>
        Task WriteAsync(byte[] data, CancellationToken cancellationToken = default);

        /// <summary>
        /// Subscribe to notifications for this characteristic.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the subscription operation</param>
        Task SubscribeAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Unsubscribe from notifications for this characteristic.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the unsubscription operation</param>
        Task UnsubscribeAsync(CancellationToken cancellationToken = default);
    }
}