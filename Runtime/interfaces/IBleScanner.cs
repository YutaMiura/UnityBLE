using System;
using System.Threading.Tasks;

namespace UnityBLE
{
    public interface IBleScanner
    {
        public BleScanEvents Events { get; }

        Task InitializeAsync();

        void StartScan(TimeSpan duration);

        void StartScan(TimeSpan duration, ScanFilter filter);
        bool StopScan();

    }
}