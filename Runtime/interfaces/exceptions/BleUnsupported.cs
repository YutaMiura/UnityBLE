using System;

namespace UnityBLE
{
    public class BleUnsupported : BleError
    {
        const string ERR_CODE = "BLE_UNSUPPORTED";

        public BleUnsupported() : base("BLE is not supported on this device", ERR_CODE, "BLE is not supported on this device")
        {
        }

        public BleUnsupported(Exception innerException) : base("BLE is not supported on this device", ERR_CODE, "BLE is not supported on this device", innerException)
        {
        }
    }
}