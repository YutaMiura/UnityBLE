using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace UnityBLE.macOS
{
    public class MacOSGetCharacteristicsCommand
    {
        public IReadOnlyList<IBleCharacteristic> Execute(MacOSBleService service, CancellationToken cancellationToken = default)
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service), "Service cannot be null");
            }

            if (string.IsNullOrEmpty(service.Uuid))
            {
                throw new ArgumentException("Service UUID cannot be null or empty", nameof(service));
            }

            if (string.IsNullOrEmpty(service.DeviceAddress))
            {
                throw new ArgumentException("Device address cannot be null or empty", nameof(service));
            }

            cancellationToken.ThrowIfCancellationRequested();

            Debug.Log($"[macOS BLE] Discovering characteristics for service {service.Uuid} on device {service.DeviceAddress}...");

            try
            {
                // Call native plugin to get characteristics
                var characteristicUuids = MacOSBleNativePlugin.GetCharacteristics(service.DeviceAddress, service.Uuid);

                if (characteristicUuids == null || characteristicUuids.Length == 0)
                {
                    Debug.LogWarning($"[macOS BLE] No characteristics found for service {service.Uuid}");
                    return new List<IBleCharacteristic>();
                }

                var characteristics = new List<IBleCharacteristic>();

                foreach (var charUuid in characteristicUuids)
                {
                    if (!string.IsNullOrEmpty(charUuid))
                    {
                        var name = CharacteristicsUtil.GetCharacteristicName(charUuid);
                        
                        // Get characteristic properties from native plugin
                        var properties = MacOSBleNativePlugin.GetCharacteristicProperties(service.DeviceAddress, service.Uuid, charUuid);

                        var characteristic = new MacOSBleCharacteristic(
                            name,
                            charUuid,
                            service,
                            properties
                        );

                        characteristics.Add(characteristic);
                    }
                }

                Debug.Log($"[macOS BLE] Discovered {characteristics.Count} characteristics for service {service.Uuid}");
                return characteristics.AsReadOnly();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[macOS BLE] Error discovering characteristics for service {service.Uuid}: {ex.Message}");
                throw;
            }
        }
    }
}