using System;
using System.Threading.Tasks;

namespace UnityBLE.iOS
{
    public sealed class iOSBleScanner : IBleScanner
    {
        public BleScanEvents Events { get; } = new BleScanEvents();
        private readonly iOSScanCommand _scanCommand;
        private readonly iOSStopScanCommand _stopCommand;

        private readonly iOSBleInitializeCommand _initializeCommand;

        public iOSBleScanner()
        {
            _scanCommand = new iOSScanCommand(Events);
            _initializeCommand = new iOSBleInitializeCommand();
            _stopCommand = new iOSStopScanCommand();
        }

        public async Task InitializeAsync()
        {
            await _initializeCommand.ExecuteAsync();
        }

        public void StartScan(TimeSpan duration)
        {
            _scanCommand.Execute(duration);
        }

        public void StartScan(TimeSpan duration, ScanFilter filter)
        {
            _scanCommand.Execute(duration, filter);
        }

        public bool StopScan()
        {
            return _stopCommand.Execute();
        }
    }
}