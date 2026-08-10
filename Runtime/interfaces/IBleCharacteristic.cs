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
        Task<string> ReadAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Write data to this characteristic.
        /// </summary>
        /// <param name="data">Data to write</param>
        /// <param name="cancellationToken">Token to cancel the write operation</param>
        Task WriteAsync(byte[] data, CancellationToken cancellationToken = default);

        /// <summary>
        /// Subscribe to notifications for this characteristic.
        /// </summary>
        /// <remarks>
        /// Returns as soon as the request has been ISSUED, not when notifications are
        /// live. Enabling notifications is an asynchronous GATT operation, and on
        /// Android the stack rejects anything sent while it is still in flight — so a
        /// command written right after this call is silently dropped. Prefer
        /// <see cref="SubscribeAsync"/> whenever a command follows the subscription.
        /// </remarks>
        void Subscribe();

        /// <summary>
        /// Subscribe to notifications and complete once the subscription is actually
        /// live on the device, so the next command cannot race it.
        /// </summary>
        /// <param name="cancellationToken">Token to stop waiting for the subscription</param>
        Task SubscribeAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Unsubscribe from notifications for this characteristic.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the unsubscription operation</param>
        Task UnsubscribeAsync();
    }
}