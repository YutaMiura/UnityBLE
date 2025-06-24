using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace UnityBLE
{
    public interface IGetCharacteristicsCommand
    {
        Task<IReadOnlyList<IBleCharacteristic>> ExecuteAsync(IBleService service, CancellationToken cancellationToken = default);
    }
}