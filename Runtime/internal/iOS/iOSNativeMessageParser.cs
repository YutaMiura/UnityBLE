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
}