using UnityEngine;

namespace UnityBLE.iOS
{
    /// <summary>
    /// iOS implementation of IBleNativeMessageParser for iOS native communication.
    /// </summary>
    public class iOSNativeMessageParser
    {
        public IBleDevice ParseDeviceData(string deviceJson)
        {
            if (string.IsNullOrEmpty(deviceJson))
            {
                Debug.LogError("[iOS BLE] Device JSON is null or empty");
                return null;
            }

            try
            {
                var deviceData = JsonUtility.FromJson<iOSDeviceData>(deviceJson);

                if (deviceData == null)
                {
                    Debug.LogError("[iOS BLE] Failed to parse device data from JSON");
                    return null;
                }

                return new iOSBleDevice(
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
                Debug.LogError($"[iOS BLE] Error parsing device JSON: {ex.Message}");
                return null;
            }
        }

        public IBleService ParseServiceData(string serviceJson)
        {
            if (string.IsNullOrEmpty(serviceJson))
            {
                Debug.LogError("[iOS BLE] Service JSON is null or empty");
                return null;
            }

            try
            {
                var serviceData = JsonUtility.FromJson<iOSServiceData>(serviceJson);

                if (serviceData == null)
                {
                    Debug.LogError("[iOS BLE] Failed to parse service data from JSON");
                    return null;
                }

                return new iOSBleService(
                    serviceData.name ?? "Unknown Service",
                    serviceData.uuid ?? "00000000-0000-0000-0000-000000000000"
                );
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[iOS BLE] Error parsing service JSON: {ex.Message}");
                return null;
            }
        }

        public IBleCharacteristic ParseCharacteristicData(string characteristicJson)
        {
            if (string.IsNullOrEmpty(characteristicJson))
            {
                Debug.LogError("[iOS BLE] Characteristic JSON is null or empty");
                return null;
            }

            try
            {
                var characteristicData = JsonUtility.FromJson<iOSCharacteristicData>(characteristicJson);

                if (characteristicData == null)
                {
                    Debug.LogError("[iOS BLE] Failed to parse characteristic data from JSON");
                    return null;
                }

                return new iOSBleCharacteristic(
                    characteristicData.name ?? "Unknown Characteristic",
                    characteristicData.uuid ?? "00000000-0000-0000-0000-000000000000",
                    characteristicData.deviceAddress ?? "",
                    characteristicData.serviceUuid ?? ""
                );
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[iOS BLE] Error parsing characteristic JSON: {ex.Message}");
                return null;
            }
        }
    }

    [System.Serializable]
    public class iOSDeviceData
    {
        public string name;
        public string address;
        public int rssi = -50;
        public bool isConnectable = true;
        public int txPower = 4;
        public string advertisingData;
    }

    [System.Serializable]
    public class iOSServiceData
    {
        public string name;
        public string uuid;
    }

    [System.Serializable]
    public class iOSCharacteristicData
    {
        public string name;
        public string uuid;
        public string deviceAddress;
        public string serviceUuid;
    }
}