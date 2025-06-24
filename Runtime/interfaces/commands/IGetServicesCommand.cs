using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace UnityBLE
{
    public interface IGetServicesCommand
    {
        Task<IReadOnlyList<IBleService>> ExecuteAsync(IBleDevice device, CancellationToken cancellationToken = default);
    }
}