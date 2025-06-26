using System;
using UnityEngine;

namespace UnityBLE.macOS
{
    public class MacOSStopScanCommand
    {
        public bool Execute()
        {
            try
            {
                MacOSBleNativePlugin.StopScan();
                Debug.Log("[macOS BLE] Scan stopped successfully");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[macOS BLE] Failed to stop scan: {ex.Message}");
                return false;
            }
        }
    }
}