using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityBLE.macOS
{
    /// <summary>
    /// macOS implementation of IBleNativeMessageParser for Unity Editor testing.
    /// </summary>
    public class MacOSNativeMessageParser
    {
        public IBleDevice ParseDeviceData(string deviceJson)
        {
            if (string.IsNullOrEmpty(deviceJson))
            {
                Debug.LogError("[macOS BLE] Device JSON is null or empty");
                return null;
            }

            try
            {
                var deviceData = JsonUtility.FromJson<MacOSDeviceData>(deviceJson);

                if (deviceData == null)
                {
                    Debug.LogError("[macOS BLE] Failed to parse device data from JSON");
                    return null;
                }

                var device = new MacOSBleDevice(
                    deviceData.name ?? "Unknown Device",
                    deviceData.address ?? "00:00:00:00:00:00",
                    deviceData.rssi,
                    deviceData.isConnectable,
                    deviceData.txPower,
                    deviceData.advertisingData ?? ""
                );

                device._services = deviceData.services != null
                    ? deviceData.services.Select(s =>
                    {
                        var service = new MacOSBleService(s.uuid, s.uuid, deviceData.address);
                        service.Characteristics = s.characteristics != null
                            ? s.characteristics.Select(c => 
                            {
                                // Get properties from native plugin for each characteristic
                                var properties = MacOSBleNativePlugin.GetCharacteristicProperties(deviceData.address, s.uuid, c);
                                return new MacOSBleCharacteristic(c, c, service, properties);
                            }).ToList<IBleCharacteristic>()
                            : new List<IBleCharacteristic>();
                        return service;
                    }).ToList<IBleService>()
                    : new List<IBleService>();

                return device;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[macOS BLE] Error parsing device JSON: {ex.Message}");
                return null;
            }
        }
    }

    [System.Serializable]
    public class MacOSDeviceData
    {
        public string name;
        public string address;
        public int rssi = -50;
        public bool isConnectable = true;
        public int txPower = 4;
        public string advertisingData;
        public ServiceData[] services;
    }

    [System.Serializable]
    public class ServiceData
    {
        public string uuid;
        public string[] characteristics;
    }
}