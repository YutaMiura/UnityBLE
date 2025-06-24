using System.Threading;
using System.Threading.Tasks;

namespace UnityBLE
{
    public interface IStopScanCommand
    {
        /// <summary>
        /// Stops the ongoing BLE scan.
        /// </summary>
        ///  <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task<bool> ExecuteAsync(CancellationToken cancellationToken = default);
    }
}