using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityBLE.iOS;

namespace UnityBLE.iOS
{
    public class iOSDisconnectDeviceCommand
    {
        public async Task<bool> ExecuteAsync(IBleDevice device, CancellationToken cancellationToken = default)
        {
            if (device == null)
            {
                Debug.LogError("[iOS BLE] Device cannot be null.");
                return false;
            }

            if (string.IsNullOrEmpty(device.Address))
            {
                Debug.LogError("[iOS BLE] Device address is not set.");
                return false;
            }

            if (!device.IsConnected)
            {
                Debug.LogWarning($"[iOS BLE] Device {device.Address} is not connected.");
                return true;
            }

            cancellationToken.ThrowIfCancellationRequested();

            Debug.Log($"[iOS BLE] Disconnecting from device {device.Address}...");

            try
            {
                // Call native disconnect
                if (!iOSBleNativePlugin.DisconnectFromDevice(device.Address))
                {
                    Debug.LogError($"[iOS BLE] Failed to start disconnection from device {device.Address}");
                    return false;
                }

                await Task.Delay(500, cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                {
                    Debug.Log($"[iOS BLE] Disconnection from device {device.Address} was cancelled.");
                    throw new OperationCanceledException();
                }

                Debug.Log($"[iOS BLE] Successfully disconnected from device {device.Address}");
                return true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[iOS BLE] Failed to disconnect from device {device.Address}: {ex.Message}");
                return false;
            }
        }
    }
}