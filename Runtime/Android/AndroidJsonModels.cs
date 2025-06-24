using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityBLE
{
    /// <summary>
    /// JSON data models for Android BLE communication.
    /// These classes are designed to work with Unity's JsonUtility for type-safe serialization.
    /// </summary>
        [Serializable]
        public class DeviceDiscoveredData
        {
            public string address = "";
            public string name = "";
            public int rssi = 0;
            public bool isConnectable = true;
            public int txPower = 0;
            public string advertisingData = "";
        }

        [Serializable]
        public class ServiceData
        {
            public string uuid = "";
            public CharacteristicData[] characteristics = new CharacteristicData[0];
        }

        [Serializable]
        public class CharacteristicData
        {
            public string uuid = "";
            public string[] properties = new string[0];
            public int permissions = 0;
            public DescriptorData[] descriptors = new DescriptorData[0];
        }

        [Serializable]
        public class DescriptorData
        {
            public string uuid = "";
            public int permissions = 0;
        }

        [Serializable]
        public class ServicesDiscoveredData
        {
            public string deviceAddress = "";
            public ServiceData[] services = new ServiceData[0];
        }

        [Serializable]
        public class BleErrorData
        {
            public string errorCode = "";
            public string message = "";
            public string details = "";
        }

        [Serializable]
        public class ScanResultData
        {
            public bool success = false;
            public string message = "";
            public int devicesFound = 0;
        }

        [Serializable]
        public class ConnectionResultData
        {
            public string deviceAddress = "";
            public bool success = false;
            public string errorMessage = "";
        }

        /// <summary>
        /// Wrapper for arrays to work with JsonUtility (which doesn't support top-level arrays)
        /// </summary>
        [Serializable]
        public class ServiceArrayWrapper
        {
            public ServiceData[] services = new ServiceData[0];
        }

        [Serializable]
        public class CharacteristicArrayWrapper
        {
            public CharacteristicData[] characteristics = new CharacteristicData[0];
        }
}