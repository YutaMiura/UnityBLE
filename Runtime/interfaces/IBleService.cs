using System.Collections.Generic;
using System.Threading.Tasks;

namespace UnityBLE
{
    /// <summary>
    /// Interface representing a BLE service.
    /// </summary>
    public interface IBleService
    {
        string Uuid { get; }
        IBleDevice Device { get; }

        /// <summary>
        /// Get the list of characteristics from this service.
        /// </summary>
        Task<IReadOnlyList<IBleCharacteristic>> GetCharacteristicsAsync();
    }
}