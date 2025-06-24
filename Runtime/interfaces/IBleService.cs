using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace UnityBLE
{
    /// <summary>
    /// Interface representing a BLE service.
    /// </summary>
    public interface IBleService
    {
        string Uuid { get; }

        /// <summary>
        /// Get the list of characteristics from this service.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the characteristics discovery operation</param>
        Task<IReadOnlyList<IBleCharacteristic>> GetCharacteristicsAsync(CancellationToken cancellationToken = default);
    }
}