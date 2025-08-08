using System;

namespace UnityBLE
{
    public class BlePowerTurnedOff : BleError
    {
        const string ERR_CODE = "BLE_POWER_TURNED_OFF";

        public BlePowerTurnedOff() : base("BLE power is turned off", ERR_CODE, "BLE power is turned off")
        {
        }

        public BlePowerTurnedOff(Exception innerException) : base("BLE power is turned off", ERR_CODE, "BLE power is turned off", innerException)
        {
        }
    }
}