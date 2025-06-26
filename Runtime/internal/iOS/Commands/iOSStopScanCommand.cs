using System;
using UnityEngine;
using UnityBLE.iOS;

namespace UnityBLE.iOS
{
    public class iOSStopScanCommand
    {
        public bool Execute()
        {
            try
            {
                iOSBleNativePlugin.StopScan();
                Debug.Log("[iOS BLE] Scan stopped successfully");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[iOS BLE] Failed to stop scan: {ex.Message}");
                return false;
            }
        }
    }
}