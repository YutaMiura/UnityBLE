using System;

namespace UnityBLE.windows
{
    public class WindowsScanCommand
    {
        public void Execute(ScanFilter filter = null)
        {
            if (!WindowsBleNativePlugin.IsInitialized)
            {
                throw new InvalidOperationException("Failed to initialize Windows BLE native plugin");
            }

            WindowsBleNativePlugin.StartScan(filter);
        }
    }
}
