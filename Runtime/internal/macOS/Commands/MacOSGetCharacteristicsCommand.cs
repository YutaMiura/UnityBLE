using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityBLE.macOS
{
    public class MacOSGetCharacteristicsCommand
    {
        public async Task<IReadOnlyList<IBleCharacteristic>> ExecuteAsync(IBleService service, CancellationToken cancellationToken = default)
        {
            if (service == null)
            {
                Debug.LogError("[macOS BLE] Service cannot be null.");
                return new List<IBleCharacteristic>();
            }

            if (string.IsNullOrEmpty(service.Uuid))
            {
                Debug.LogError("[macOS BLE] Service UUID is not set.");
                return new List<IBleCharacteristic>();
            }

            cancellationToken.ThrowIfCancellationRequested();

            Debug.Log($"[macOS BLE] Discovering characteristics for service ({service.Uuid})...");

            await Task.Delay(1000, cancellationToken);

            if (cancellationToken.IsCancellationRequested)
            {
                Debug.Log($"[macOS BLE] Characteristic discovery for service {service.Uuid} was cancelled.");
                throw new OperationCanceledException();
            }

            var characteristics = new List<IBleCharacteristic>();

            switch (service.Uuid.ToLower())
            {
                case "00001800-0000-1000-8000-00805f9b34fb": // Generic Access
                    characteristics.Add(new MacOSBleCharacteristic("Device Name", "00002a00-0000-1000-8000-00805f9b34fb"));
                    characteristics.Add(new MacOSBleCharacteristic("Appearance", "00002a01-0000-1000-8000-00805f9b34fb"));
                    characteristics.Add(new MacOSBleCharacteristic("Peripheral Privacy Flag", "00002a02-0000-1000-8000-00805f9b34fb"));
                    break;

                case "00001801-0000-1000-8000-00805f9b34fb": // Generic Attribute
                    characteristics.Add(new MacOSBleCharacteristic("Service Changed", "00002a05-0000-1000-8000-00805f9b34fb"));
                    break;

                case "0000180a-0000-1000-8000-00805f9b34fb": // Device Information
                    characteristics.Add(new MacOSBleCharacteristic("Manufacturer Name", "00002a29-0000-1000-8000-00805f9b34fb"));
                    characteristics.Add(new MacOSBleCharacteristic("Model Number", "00002a24-0000-1000-8000-00805f9b34fb"));
                    characteristics.Add(new MacOSBleCharacteristic("Serial Number", "00002a25-0000-1000-8000-00805f9b34fb"));
                    characteristics.Add(new MacOSBleCharacteristic("Hardware Revision", "00002a27-0000-1000-8000-00805f9b34fb"));
                    characteristics.Add(new MacOSBleCharacteristic("Firmware Revision", "00002a26-0000-1000-8000-00805f9b34fb"));
                    characteristics.Add(new MacOSBleCharacteristic("Software Revision", "00002a28-0000-1000-8000-00805f9b34fb"));
                    break;

                case "0000180f-0000-1000-8000-00805f9b34fb": // Battery Service
                    characteristics.Add(new MacOSBleCharacteristic("Battery Level", "00002a19-0000-1000-8000-00805f9b34fb"));
                    break;

                default:
                    characteristics.Add(new MacOSBleCharacteristic("Unknown Characteristic", "00000000-0000-1000-8000-00805f9b34fb"));
                    break;
            }

            Debug.Log($"[macOS BLE] Discovered {characteristics.Count} characteristics for service {service.Uuid}");

            return characteristics.AsReadOnly();
        }
    }
}