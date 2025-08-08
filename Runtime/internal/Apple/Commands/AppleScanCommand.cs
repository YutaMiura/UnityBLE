using System;

namespace UnityBLE.apple
{
    public class AppleScanCommand
    {
        public void Execute(ScanFilter filter = null)
        {
            // Initialize native plugin if not already done
            if (!AppleBleNativePlugin.IsInitialized)
            {
                throw new InvalidOperationException("Failed to initialize apple BLE native plugin");
            }

            AppleBleNativePlugin.StartScan(filter);
        }
    }
}