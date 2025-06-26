using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityBLE.iOS;

namespace UnityBLE.iOS
{
    public class iOSGetCharacteristicsCommand
    {
        public async Task<IReadOnlyList<IBleCharacteristic>> ExecuteAsync(IBleService service, string deviceAddress, CancellationToken cancellationToken = default)
        {
            if (service == null)
            {
                Debug.LogError("[iOS BLE] Service cannot be null.");
                return new List<IBleCharacteristic>();
            }

            if (string.IsNullOrEmpty(service.Uuid))
            {
                Debug.LogError("[iOS BLE] Service UUID is not set.");
                return new List<IBleCharacteristic>();
            }

            if (string.IsNullOrEmpty(deviceAddress))
            {
                Debug.LogError("[iOS BLE] Device address is not set.");
                return new List<IBleCharacteristic>();
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Initialize native plugin if not already done
            if (!iOSBleNativePlugin.Initialize())
            {
                Debug.LogError("[iOS BLE] Failed to initialize iOS BLE native plugin");
                return new List<IBleCharacteristic>();
            }

            Debug.Log($"[iOS BLE] Discovering characteristics for service ({service.Uuid})...");

            try
            {
                // Get characteristics from native plugin
                string[] characteristicUUIDs = iOSBleNativePlugin.GetCharacteristics(deviceAddress, service.Uuid);

                if (characteristicUUIDs == null || characteristicUUIDs.Length == 0)
                {
                    Debug.LogWarning($"[iOS BLE] No characteristics found for service {service.Uuid}");
                    return new List<IBleCharacteristic>();
                }

                var characteristics = new List<IBleCharacteristic>();
                foreach (var uuid in characteristicUUIDs)
                {
                    // Try to get a friendly name for the characteristic
                    string characteristicName = GetCharacteristicName(uuid);
                    characteristics.Add(new iOSBleCharacteristic(characteristicName, uuid, deviceAddress, service.Uuid));
                }

                Debug.Log($"[iOS BLE] Discovered {characteristics.Count} characteristics for service {service.Uuid}");

                return characteristics.AsReadOnly();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[iOS BLE] Error discovering characteristics: {ex.Message}");
                return new List<IBleCharacteristic>();
            }
        }

        private string GetCharacteristicName(string uuid)
        {
            // Convert to standard UUID format for comparison
            string standardUuid = uuid.ToUpper();

            return standardUuid switch
            {
                "00002A00-0000-1000-8000-00805F9B34FB" => "Device Name",
                "00002A01-0000-1000-8000-00805F9B34FB" => "Appearance",
                "00002A02-0000-1000-8000-00805F9B34FB" => "Peripheral Privacy Flag",
                "00002A05-0000-1000-8000-00805F9B34FB" => "Service Changed",
                "00002A19-0000-1000-8000-00805F9B34FB" => "Battery Level",
                "00002A24-0000-1000-8000-00805F9B34FB" => "Model Number",
                "00002A25-0000-1000-8000-00805F9B34FB" => "Serial Number",
                "00002A26-0000-1000-8000-00805F9B34FB" => "Firmware Revision",
                "00002A27-0000-1000-8000-00805F9B34FB" => "Hardware Revision",
                "00002A28-0000-1000-8000-00805F9B34FB" => "Software Revision",
                "00002A29-0000-1000-8000-00805F9B34FB" => "Manufacturer Name",
                _ => $"Unknown Characteristic ({uuid})"
            };
        }
    }
}