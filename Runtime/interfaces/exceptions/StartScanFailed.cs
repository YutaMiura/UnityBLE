using System;

namespace UnityBLE
{
    public class StartScanFailed : BleError
    {
        const string ERR_CODE = "START_SCAN_FAILED";
        public StartScanFailed() : base("Failed to start BLE scan", ERR_CODE, "Failed to start BLE scan")
        {
        }

        public StartScanFailed(Exception innerException) : base("Failed to start BLE scan", ERR_CODE, "Failed to start BLE scan", innerException)
        {
        }
    }
}