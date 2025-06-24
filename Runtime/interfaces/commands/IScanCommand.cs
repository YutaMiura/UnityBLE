using System;
using System.Threading;
using System.Threading.Tasks;

namespace UnityBLE
{
    public interface IScanCommand
    {
        Task ExecuteAsync(TimeSpan duration, CancellationToken cancellationToken = default);

        Task ExecuteAsync(TimeSpan duration, ScanFilter filter, CancellationToken cancellationToken = default);
    }

    public class ScanFilter
    {
        public string[] ServiceUuids { get; set; }
        public string[] Names { get; set; }

        public ScanFilter(string[] serviceUuids = null, string[] names = null)
        {
            ServiceUuids = serviceUuids ?? Array.Empty<string>();
            Names = names ?? Array.Empty<string>();
        }
    }
}