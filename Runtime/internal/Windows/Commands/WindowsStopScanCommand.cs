using System;
using UnityEngine;

namespace UnityBLE.windows
{
    public class WindowsStopScanCommand
    {
        public bool Execute()
        {
            try
            {
                WindowsBleNativePlugin.StopScan();
                Debug.Log(" Scan stopped successfully");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($" Failed to stop scan: {ex.Message}");
                return false;
            }
        }
    }
}
