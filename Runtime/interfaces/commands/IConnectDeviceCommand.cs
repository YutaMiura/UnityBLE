using System.Threading;
using System.Threading.Tasks;

namespace UnityBLE
{
    public interface IConnectDeviceCommand
    {
        /// <summary>
        /// Connects to the specified BLE device.
        /// </summary>
        /// <param name="address">The address of the BLE device to connect to.</param>
        /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task<IBleDevice> ExecuteAsync(string address, CancellationToken cancellationToken = default);
    }
}