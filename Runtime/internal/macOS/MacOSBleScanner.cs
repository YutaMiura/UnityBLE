using System;
using System.Threading.Tasks;
using UnityBle.macOS;

namespace UnityBLE.macOS
{
    public sealed class MacOSBleScanner : IBleScanner
    {
        public BleScanEvents Events { get; } = new BleScanEvents();
        private readonly MacOSScanCommand _scanCommand;
        private readonly MacOSStopScanCommand _stopCommand;

        private readonly MacOSBleInitializeCommand _initializeCommand;

        public MacOSBleScanner()
        {
            _scanCommand = new MacOSScanCommand(Events);
            _initializeCommand = new MacOSBleInitializeCommand();
            _stopCommand = new MacOSStopScanCommand();
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