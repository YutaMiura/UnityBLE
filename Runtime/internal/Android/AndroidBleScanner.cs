using System;
using System.Threading.Tasks;

namespace UnityBLE.Android
{
    public sealed class AndroidBleScanner : IBleScanner
    {
        public BleScanEvents Events { get; } = new BleScanEvents();

        private readonly AndroidBlePermissionService _permissionService;
        private readonly AndroidScanCommand _scanCommand;
        private readonly AndroidStopScanCommand _stopCommand;

        public AndroidBleScanner()
        {
            _permissionService = new AndroidBlePermissionService();
            _scanCommand = new AndroidScanCommand(Events);
            _stopCommand = new AndroidStopScanCommand();
        }

        public async Task InitializeAsync()
        {
            if (!_permissionService.ArePermissionsGranted())
            {
                await _permissionService.RequestPermissionsAsync();
            }
        }

        public void StartScan(TimeSpan duration)
        {
            if (!_permissionService.ArePermissionsGranted())
            {
                throw new InvalidOperationException("Permissions not granted. Call RequestPermissionAsync first.");
            }
            if (duration <= TimeSpan.Zero)
            {
                throw new ArgumentException("Scan duration must be greater than zero.", nameof(duration));
            }

            _scanCommand.Execute(duration);
        }

        public void StartScan(TimeSpan duration, ScanFilter filter)
        {
            if (!_permissionService.ArePermissionsGranted())
            {
                throw new InvalidOperationException("Permissions not granted. Call RequestPermissionAsync first.");
            }
            if (duration <= TimeSpan.Zero)
            {
                throw new ArgumentException("Scan duration must be greater than zero.", nameof(duration));
            }

            _scanCommand.Execute(duration, filter);
        }

        public bool StopScan()
        {
            return _stopCommand.Execute();
        }
    }
}