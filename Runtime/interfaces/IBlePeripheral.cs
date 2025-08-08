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
        /// Connect to this BLE device.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the connection operation</param>
        Task ConnectAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Disconnect from this BLE device.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the disconnection operation</param>
        Task DisconnectAsync(CancellationToken cancellationToken = default);
    }
}