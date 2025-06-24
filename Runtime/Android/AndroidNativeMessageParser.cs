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
    internal sealed class AndroidNativeMessageParser : IBleNativeMessageParser
    {
        /// <summary>
        /// Parse connection result JSON data.
        /// </summary>
        /// <param name="connectionJson">JSON string containing connection result</param>
        /// <returns>Parsed connection result data</returns>
        public ConnectionResultData ParseConnectionData(string connectionJson)
        {
            if (string.IsNullOrEmpty(connectionJson))
            {
                return new ConnectionResultData { success = false, errorMessage = "Empty connection data" };
            }

            try
            {
                return JsonUtility.FromJson<ConnectionResultData>(connectionJson);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to parse connection JSON: {ex.Message}");
                return new ConnectionResultData
                {
                    success = false,
                    errorMessage = $"Parse error: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Parse scan result JSON data.
        /// </summary>
        /// <param name="scanJson">JSON string containing scan result</param>
        /// <returns>Parsed scan result data</returns>
        public ScanResultData ParseScanResultData(string scanJson)
        {
            if (string.IsNullOrEmpty(scanJson))
            {
                return new ScanResultData { success = false, message = "Empty scan result" };
            }

            try
            {
                return JsonUtility.FromJson<ScanResultData>(scanJson);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to parse scan result JSON: {ex.Message}");
                return new ScanResultData
                {
                    success = false,
                    message = $"Parse error: {ex.Message}"
                };
            }
        }

        IBleDevice IBleNativeMessageParser.ParseDeviceData(string deviceJson)
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

        IReadOnlyList<IBleService> IBleNativeMessageParser.ParseServicesData(string servicesJson)
        {
            if (string.IsNullOrEmpty(servicesJson))
            {
                throw new JsonException("Services JSON is null or empty");
            }

            try
            {
                var servicesData = JsonUtility.FromJson<ServicesDiscoveredData>(servicesJson);

                // Validate required fields
                if (string.IsNullOrEmpty(servicesData.deviceAddress))
                {
                    throw new JsonException("Device address is missing in services data");
                }

                if (servicesData?.services == null)
                {
                    return new List<IBleService>();
                }

                var services = new List<IBleService>();

                foreach (var serviceData in servicesData.services)
                {
                    if (!string.IsNullOrEmpty(serviceData.uuid))
                    {
                        var service = new AndroidBleService(serviceData.uuid);

                        // Add characteristics if present
                        if (serviceData.characteristics != null)
                        {
                            foreach (var charData in serviceData.characteristics)
                            {
                                if (!string.IsNullOrEmpty(charData.uuid))
                                {
                                    var characteristic = new AndroidBleCharacteristic(
                                        charData.uuid,
                                        service
                                    );

                                    // Note: Characteristics would be added to service in a more complete implementation
                                }
                            }
                        }

                        services.Add(service);
                    }
                }

                return services;
            }
            catch (ArgumentException ex)
            {
                throw new JsonException($"Invalid JSON format for services data: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new JsonException($"Unexpected error parsing services JSON: {ex.Message}", ex);
            }
        }

        Exception IBleNativeMessageParser.ParseErrorMessage(string errorJson)
        {
            if (string.IsNullOrEmpty(errorJson))
            {
                return new JsonException("JSON is null or empty.");
            }

            try
            {
                var err = JsonUtility.FromJson<BleErrorData>(errorJson);
                return new BleError
                (
                    message: err.message ?? "Unknown error",
                    errorCode: err.errorCode ?? "UNKNOWN_ERROR",
                    details: err.details ?? "No additional details"
                );
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to parse error JSON, using fallback: {ex.Message}");
                return ex;
            }
        }
    }
}