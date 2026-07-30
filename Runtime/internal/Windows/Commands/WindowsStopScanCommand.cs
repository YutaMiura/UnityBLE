using System;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityBLE.windows
{
    public class WindowsStopScanCommand
    {
        // Blocks until the native advertisement watcher has actually stopped
        // (up to the native timeout). Prefer ExecuteAsync from the Unity main
        // thread so the wait does not stall a frame.
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

        public Task<bool> ExecuteAsync()
        {
            return Task.Run(Execute);
        }
    }
}
