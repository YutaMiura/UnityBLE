using System;
using System.Threading;
using System.Threading.Tasks;

namespace UnityBLE
{
    public class BleScanner
    {
        private static BleScanner _instance;
        public static BleScanner Instance => _instance ??= new BleScanner();

        public readonly BleScanEvents Events;

        private readonly IBlePermissionService _permissionService;

        private BleScanner()
        {
            Events = new BleScanEvents();
            _permissionService = BlePermissionServiceFactory.Create();
        }

        public async Task StartScan(TimeSpan duration, CancellationToken cancellationToken = default)
        {
            if (!_permissionService.ArePermissionsGranted())
            {
                await _permissionService.RequestPermissionsAsync();
            }
            await BleCommandFactory.CreateStartScanCommand().ExecuteAsync(duration, cancellationToken);
        }

        public async Task StartScanByServiceUuids(TimeSpan duration, params string[] serviceUuids)
        {
            if (!_permissionService.ArePermissionsGranted())
            {
                await _permissionService.RequestPermissionsAsync();
            }
            await BleCommandFactory.CreateStartScanCommand().ExecuteAsync(duration, new ScanFilter(serviceUuids));
        }

        public async Task StartScanByNames(TimeSpan duration, params string[] names)
        {
            if (!_permissionService.ArePermissionsGranted())
            {
                await _permissionService.RequestPermissionsAsync();
            }
            await BleCommandFactory.CreateStartScanCommand().ExecuteAsync(duration, new ScanFilter(names));
        }

        public async Task<bool> StopScanAsync(CancellationToken cancellationToken = default)
        {
            return await BleCommandFactory.CreateStopScanCommand().ExecuteAsync(cancellationToken);
        }
    }
}