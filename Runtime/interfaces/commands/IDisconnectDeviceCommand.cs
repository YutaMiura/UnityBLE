using System.Threading;
using System.Threading.Tasks;

namespace UnityBLE
{
    public interface IDisconnectDeviceCommand
    {
        /// <summary>
        /// Disconnects from the specified BLE device.
        /// </summary>
        /// <param name="device">The BLE device to disconnect from.</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task<bool> ExecuteAsync(IBleDevice device, CancellationToken cancellationToken = default);
    }
}