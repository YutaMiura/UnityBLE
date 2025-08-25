using System;
using System.Threading;
using System.Threading.Tasks;

namespace UnityBLE
{
    /// <summary>
    /// Interface representing a BLE characteristic.
    /// </summary>
    public interface IBleCharacteristic : IDisposable
    {
        string peripheralUUID { get; }
        string serviceUUID { get; }
        string Uuid { get; }
        CharacteristicProperties Properties { get; }

        public delegate void DataReceivedDelegate(string data);
        public event DataReceivedDelegate OnDataReceived;

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
        void Subscribe();

        /// <summary>
        /// Unsubscribe from notifications for this characteristic.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the unsubscription operation</param>
        Task UnsubscribeAsync();
    }
}