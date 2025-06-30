using System;
using System.Collections.Generic;
using Unity.Plastic.Newtonsoft.Json;
using UnityEngine;

namespace UnityBLE
{
    /// <summary>
    /// Type-safe JSON parser for Android BLE communication using Unity's JsonUtility.
    /// Replaces manual string parsing with robust, maintainable serialization.
    /// </summary>
    internal sealed class AndroidNativeMessageParser
    {
        public IBleDevice ParseDeviceData(string deviceJson)
        {
            if (string.IsNullOrEmpty(deviceJson))
            {
                throw new JsonException("Device JSON is null or empty");
            }


            try
            {
                var deviceData = JsonUtility.FromJson<DeviceDiscoveredData>(deviceJson);

                // Validate required fields
                if (string.IsNullOrEmpty(deviceData.address))
                {
                    throw new JsonException("Device address is missing or empty");
                }

                return new AndroidBleDevice(
                    deviceData.name ?? "",
                    deviceData.address,
                    deviceData.rssi,
                    deviceData.isConnectable,
                    deviceData.txPower,
                    deviceData.advertisingData ?? ""
                );
            }
            catch (ArgumentException ex)
            {
                throw new JsonException($"Invalid JSON format for device data: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new JsonException($"Unexpected error parsing device JSON: {ex.Message}", ex);
            }
        }
    }
}