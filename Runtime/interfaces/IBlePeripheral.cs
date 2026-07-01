using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace UnityBLE
{
    /// <summary>
    /// Interface representing a BLE peripheral.
    /// </summary>
    public interface IBlePeripheral : IDisposable
    {
        string Name { get; }
        string UUID { get; }

        public bool IsConnected { get; }

        public delegate void ConnectionStatusChangedDelegate(IBlePeripheral device, bool isConnected);
        public event ConnectionStatusChangedDelegate OnConnectionStatusChanged;

        public delegate void OnServiceDiscoveredDelegate(IBleService service);
        public event OnServiceDiscoveredDelegate OnServiceDiscovered;

        public IEnumerable<IBleService> Services { get; }

        /// <summary>
        /// Connect to this BLE device, bounding the attempt with the library's default connect
        /// timeout. The native connect has no timeout of its own on any platform, so the library
        /// always enforces one; on expiry a <see cref="System.TimeoutException"/> is thrown.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the connection operation</param>
        // NOTE: kept as two overloads rather than one method with an optional TimeSpan? so that
        // expression-tree call sites (e.g. Moq's Setup(m => m.ConnectAsync(It.IsAny<...>()))) keep
        // compiling - an expression tree may not omit an optional argument (CS0854).
        Task ConnectAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Connect to this BLE device, bounding the attempt with an explicit
        /// <paramref name="timeout"/>. On expiry the attempt is aborted and a
        /// <see cref="System.TimeoutException"/> is thrown.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the connection operation</param>
        /// <param name="timeout">Maximum time to wait for the connection to establish.</param>
        Task ConnectAsync(CancellationToken cancellationToken, TimeSpan timeout);

        /// <summary>
        /// Disconnect from this BLE device.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the disconnection operation</param>
        Task DisconnectAsync(CancellationToken cancellationToken = default);
    }
}