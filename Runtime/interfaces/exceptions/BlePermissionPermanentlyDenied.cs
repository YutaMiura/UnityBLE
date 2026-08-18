using System;

namespace UnityBLE
{
    public class BlePermissionPermanentlyDenied : BleError
    {
        private const string ERR_CODE = "BLE_PERMISSION_PERMANENTLY_DENIED";

        public BlePermissionPermanentlyDenied()
            : base(
                "BLE permission was permanently denied",
                ERR_CODE,
                "BLE permission cannot be requested again. Open the app settings to grant it.")
        {
        }

        public BlePermissionPermanentlyDenied(Exception innerException)
            : base(
                "BLE permission was permanently denied",
                ERR_CODE,
                "BLE permission cannot be requested again. Open the app settings to grant it.",
                innerException)
        {
        }
    }
}
