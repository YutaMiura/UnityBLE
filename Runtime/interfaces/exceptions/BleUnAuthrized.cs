using System;

namespace UnityBLE
{
    public class BleUnAuthorized : BleError
    {
        const string ERR_CODE = "BLE_UNAUTHORIZED";

        public BleUnAuthorized() : base("Unauthorized access to BLE", ERR_CODE, "Unauthorized access to BLE")
        {
        }

        public BleUnAuthorized(Exception innerException) : base("Unauthorized access to BLE", ERR_CODE, "Unauthorized access to BLE", innerException)
        {
        }
    }
}