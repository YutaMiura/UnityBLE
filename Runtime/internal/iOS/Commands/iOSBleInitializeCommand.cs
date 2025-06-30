using System;
using System.Threading.Tasks;
using UnityBLE.iOS;

namespace UnityBLE.iOS
{
    public class iOSBleInitializeCommand
    {
        private IOSBlePermissionService _permissionService;

        public iOSBleInitializeCommand()
        {
            _permissionService = new IOSBlePermissionService();
        }

        public async Task ExecuteAsync()
        {
            if (_permissionService == null)
            {
                throw new InvalidOperationException("iOSBleInitializeCommand is not initialized properly.");
            }

            if (!_permissionService.ArePermissionsGranted())
            {
                if (!await _permissionService.RequestPermissionsAsync())
                {
                    throw new InvalidOperationException("BLE permissions are not granted. Cannot initialize BLE.");
                }
            }

            if (iOSBleNativePlugin.IsBluetoothReady())
            {
                return;
            }

            iOSBleNativePlugin.Initialize();
        }
    }
}