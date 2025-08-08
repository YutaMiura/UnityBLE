using System;
using UnityEngine;

namespace UnityBLE.apple
{
    public class AppleStopScanCommand
    {
        public bool Execute()
        {
            try
            {
                AppleBleNativePlugin.StopScan();
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