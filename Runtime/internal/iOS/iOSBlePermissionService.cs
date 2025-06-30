using System;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityBLE.iOS
{
    /// <summary>
    /// iOS BLE permission service implementation.
    /// On iOS, Bluetooth permissions are handled automatically by the system
    /// when first attempting to use Core Bluetooth functionality.
    /// </summary>
    public class IOSBlePermissionService : IBlePermissionService
    {
        public bool ArePermissionsGranted()
        {
            // On iOS, we check the Bluetooth state through the native plugin
            // If the plugin is initialized and Bluetooth is ready, permissions are granted
            try
            {
                return iOSBleNativePlugin.IsBluetoothReady();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[IOSBlePermissionService] Error checking Bluetooth state: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> RequestPermissionsAsync()
        {
            try
            {
                if (!ArePermissionsGranted())
                {
                    Debug.Log("[IOSBlePermissionService] Requesting Bluetooth permissions...");
                    // Request permissions through initialization
                    iOSBleNativePlugin.Initialize();
                }

                // Wait for Bluetooth to be ready with a reasonable timeout
                // This will trigger the system permission dialog if needed
                bool isReady = await WaitForBluetoothReadyAsync(10.0f); // 10 second timeout

                if (isReady)
                {
                    Debug.Log("[IOSBlePermissionService] Bluetooth permissions granted and ready");
                    return true;
                }
                else
                {
                    Debug.LogWarning("[IOSBlePermissionService] Bluetooth not ready - permissions may have been denied or Bluetooth is disabled");
                    return false;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[IOSBlePermissionService] Exception requesting permissions: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> WaitForBluetoothReadyAsync(float timeoutSeconds)
        {
            var elapsedTime = 0d;
            var checkInterval = TimeSpan.FromMilliseconds(100);

            while (elapsedTime < timeoutSeconds)
            {
                if (ArePermissionsGranted())
                {
                    return true;
                }

                await Task.Delay(checkInterval);
                elapsedTime += checkInterval.TotalSeconds;
            }

            Debug.LogWarning($"[IOSBlePermissionService] Timeout waiting for Bluetooth after {timeoutSeconds} seconds");
            return false;
        }
    }
}