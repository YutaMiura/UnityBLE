using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityBLE.iOS;

namespace UnityBLE.iOS
{
    public class iOSGetServicesCommand
    {
        public async Task<IReadOnlyList<IBleService>> ExecuteAsync(IBleDevice device, CancellationToken cancellationToken = default)
        {
            if (device == null)
            {
                Debug.LogError("[iOS BLE] Device cannot be null.");
                return new List<IBleService>();
            }

            if (string.IsNullOrEmpty(device.Address))
            {
                Debug.LogError("[iOS BLE] Device address is not set.");
                return new List<IBleService>();
            }

            if (!device.IsConnected)
            {
                Debug.LogError($"[iOS BLE] Device {device.Address} is not connected.");
                return new List<IBleService>();
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Initialize native plugin if not already done
            if (!iOSBleNativePlugin.Initialize())
            {
                Debug.LogError("[iOS BLE] Failed to initialize iOS BLE native plugin");
                return new List<IBleService>();
            }

            Debug.Log($"[iOS BLE] Discovering services for device {device.Address}...");

            try
            {
                // Get services from native plugin
                string[] serviceUUIDs = iOSBleNativePlugin.GetServices(device.Address);

                if (serviceUUIDs == null || serviceUUIDs.Length == 0)
                {
                    Debug.LogWarning($"[iOS BLE] No services found for device {device.Address}");
                    return new List<IBleService>();
                }

                var services = new List<IBleService>();
                foreach (var uuid in serviceUUIDs)
                {
                    // Try to get a friendly name for the service
                    string serviceName = GetServiceName(uuid);
                    services.Add(new iOSBleService(serviceName, uuid));
                }

                Debug.Log($"[iOS BLE] Discovered {services.Count} services for device {device.Address}");

                return services.AsReadOnly();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[iOS BLE] Error discovering services: {ex.Message}");
                return new List<IBleService>();
            }
        }

        private string GetServiceName(string uuid)
        {
            // Convert to standard UUID format for comparison
            string standardUuid = uuid.ToUpper();

            return standardUuid switch
            {
                "00001800-0000-1000-8000-00805F9B34FB" => "Generic Access",
                "00001801-0000-1000-8000-00805F9B34FB" => "Generic Attribute",
                "0000180A-0000-1000-8000-00805F9B34FB" => "Device Information",
                "0000180F-0000-1000-8000-00805F9B34FB" => "Battery Service",
                "0000180D-0000-1000-8000-00805F9B34FB" => "Running Speed and Cadence",
                "0000180E-0000-1000-8000-00805F9B34FB" => "Phone Alert Status Service",
                "00001802-0000-1000-8000-00805F9B34FB" => "Immediate Alert",
                "00001803-0000-1000-8000-00805F9B34FB" => "Link Loss",
                "00001804-0000-1000-8000-00805F9B34FB" => "Tx Power",
                "00001805-0000-1000-8000-00805F9B34FB" => "Current Time Service",
                "00001806-0000-1000-8000-00805F9B34FB" => "Reference Time Update Service",
                "00001807-0000-1000-8000-00805F9B34FB" => "Next DST Change Service",
                "00001808-0000-1000-8000-00805F9B34FB" => "Glucose",
                "00001809-0000-1000-8000-00805F9B34FB" => "Health Thermometer",
                "0000180B-0000-1000-8000-00805F9B34FB" => "Network Availability",
                "0000180C-0000-1000-8000-00805F9B34FB" => "Watchdog",
                _ => $"Unknown Service ({uuid})"
            };
        }
    }
}