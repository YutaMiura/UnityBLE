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

                return new MacOSBleDevice(
                    deviceData.name ?? "Unknown Device",
                    deviceData.address ?? "00:00:00:00:00:00",
                    deviceData.rssi,
                    deviceData.isConnectable,
                    deviceData.txPower,
                    deviceData.advertisingData ?? ""
                );
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[macOS BLE] Error parsing device JSON: {ex.Message}");
                return null;
            }
        }

        public IBleService ParseServiceData(string serviceJson)
        {
            if (string.IsNullOrEmpty(serviceJson))
            {
                Debug.LogError("[macOS BLE] Service JSON is null or empty");
                return null;
            }

            try
            {
                var serviceData = JsonUtility.FromJson<MacOSServiceData>(serviceJson);

                if (serviceData == null)
                {
                    Debug.LogError("[macOS BLE] Failed to parse service data from JSON");
                    return null;
                }

                return new MacOSBleService(
                    serviceData.name ?? "Unknown Service",
                    serviceData.uuid ?? "00000000-0000-0000-0000-000000000000"
                );
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[macOS BLE] Error parsing service JSON: {ex.Message}");
                return null;
            }
        }

        public IBleCharacteristic ParseCharacteristicData(string characteristicJson)
        {
            if (string.IsNullOrEmpty(characteristicJson))
            {
                Debug.LogError("[macOS BLE] Characteristic JSON is null or empty");
                return null;
            }

            try
            {
                var characteristicData = JsonUtility.FromJson<MacOSCharacteristicData>(characteristicJson);

                if (characteristicData == null)
                {
                    Debug.LogError("[macOS BLE] Failed to parse characteristic data from JSON");
                    return null;
                }

                return new MacOSBleCharacteristic(
                    characteristicData.name ?? "Unknown Characteristic",
                    characteristicData.uuid ?? "00000000-0000-0000-0000-000000000000"
                );
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[macOS BLE] Error parsing characteristic JSON: {ex.Message}");
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
    }

    [System.Serializable]
    public class MacOSServiceData
    {
        public string name;
        public string uuid;
    }

    [System.Serializable]
    public class MacOSCharacteristicData
    {
        public string name;
        public string uuid;
    }
}