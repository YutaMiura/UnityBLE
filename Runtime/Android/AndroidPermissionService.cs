using System.Threading.Tasks;

namespace UnityBLE
{
    public static class AndroidPermissionService
    {
        public static async Task<bool> RequestBluetoothPermissions()
        {
            var apiLevel = AndroidVersion.ApiLevel;

            if (!HasAllPermissions(BluetoothPermissions(apiLevel)))
            {
                RequestPermissions(BluetoothPermissions(apiLevel));
                await Task.Delay(1000);

                if (!HasAllPermissions(BluetoothPermissions(apiLevel)))
                {
                    return false;
                }
            }

            return true;
        }

        private static string[] BluetoothPermissions(int apiLevel)
        {
            if (apiLevel >= 31) // Android 12 and above (API Level 31+)
            {
                return new string[] {
                    "android.permission.BLUETOOTH_SCAN",
                    "android.permission.BLUETOOTH_CONNECT"
                };
            }
            else if (apiLevel == 29 || apiLevel == 30)
            {
                return new string[] {
                    "android.permission.ACCESS_FINE_LOCATION",
                    "android.permission.BLUETOOTH",
                    "android.permission.BLUETOOTH_ADMIN"
                };
            }
            else // Android 11 and below (API Level 30 and below)
            {
                return new string[] {
                    "android.permission.BLUETOOTH",
                    "android.permission.BLUETOOTH_ADMIN"
                };
            }
        }

        private static bool HasAllPermissions(string[] permissions)
        {
            foreach (var permission in permissions)
            {
                if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(permission))
                {
                    return false;
                }
            }
            return true;
        }

        private static void RequestPermissions(string[] permissions)
        {
            foreach (var permission in permissions)
            {
                UnityEngine.Android.Permission.RequestUserPermission(permission);
            }
        }
    }
}