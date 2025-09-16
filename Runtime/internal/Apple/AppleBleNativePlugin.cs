#if UNITY_EDITOR_OSX || UNITY_IOS
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using AOT;
using UnityBLE.apple;
using UnityEngine;

namespace UnityBLE
{
    /// <summary>
    /// Unity bridge for macOS Core Bluetooth native plugin.
    /// </summary>
    public static class AppleBleNativePlugin
    {
        // Delegates for callbacks from native code
        public delegate void OnPeripheralFoundDelegate(string deviceJson);
        public delegate void ScanCompletedDelegate();
        public delegate void OnConnectedDelegate(string peripheralUUID);
        public delegate void OnDisconnectedDelegate(string peripheralUUID);
        public delegate void OnBleStatusChangedDelegate(int status);
        public delegate void OnBleErrorDelegate(string errorJson);
        public delegate void OnClearFoundDevicesDelegate();

        public delegate void OnServiceFoundDelegate(string serviceJson);
        public delegate void OnCharacteristicFoundDelegate(string characteristicJson);

        public delegate void OnLogReceivedDelegate(string logJson);

        public delegate void OnWriteCharacteristicCompletedDelegate(string characteristicUUID, int result, string errorDescription);
        public delegate void OnReadRSSICompletedDelegate(int result, string errorDescription);

        public delegate void OnDataReceivedDelegate(string peripheralUuid, string data);

        private static List<IBlePeripheral> _discoveredPeripherals = new List<IBlePeripheral>();

        // Native function imports
        [DllImport("__Internal")]
        private static extern int UnityBLE_StartScanning(string[] serviceUUIDs, string nameFilter);

        [DllImport("__Internal")]
        private static extern void UnityBLE_StopScanning();

        [DllImport("__Internal")]
        private static extern bool UnityBLE_IsScanning();

        [DllImport("__Internal")]
        private static extern int UnityBLE_ConnectToPeripheral(string peripheralUUID);

        [DllImport("__Internal")]
        private static extern int UnityBLE_DisconnectFromPeripheral(string peripheralUUID);

        [DllImport("__Internal")]
        private static extern int UnityBLE_ReadCharacteristic(string peripheralUUID, string serviceUUID, string characteristicUUID);

        [DllImport("__Internal")]
        private static extern int UnityBLE_WriteCharacteristic(string peripheralUUID, string serviceUUID, string characteristicUUID, byte[] data, int length);

        [DllImport("__Internal")]
        private static extern int UnityBLE_SubscribeToCharacteristic(string peripheralUUID, string serviceUUID, string characteristicUUID);

        [DllImport("__Internal")]
        private static extern int UnityBLE_UnsubscribeFromCharacteristic(string peripheralUUID, string serviceUUID, string characteristicUUID);

        [DllImport("__Internal")]
        private static extern void UnityBLE_registerOnPeripheralDiscovered(OnPeripheralFoundDelegate callback);

        [DllImport("__Internal")]
        private static extern void UnityBLE_registerOnPeripheralConnected(OnConnectedDelegate callback);

        [DllImport("__Internal")]
        private static extern void UnityBLE_registerOnPeripheralDisconnected(OnDisconnectedDelegate callback);

        [DllImport("__Internal")]
        private static extern void UnityBLE_registerOnBleErrorDetected(OnBleErrorDelegate callback);

        [DllImport("__Internal")]
        private static extern void UnityBLE_registerOnBleStateChanged(OnBleStatusChangedDelegate callback);

        [DllImport("__Internal")]
        private static extern void UnityBLE_registerOnDiscoveredPeripheralCleared(OnClearFoundDevicesDelegate callback);

        [DllImport("__Internal")]
        private static extern void UnityBLE_registerOnDiscoveredServices(OnServiceFoundDelegate callback);

        [DllImport("__Internal")]
        private static extern void UnityBLE_registerOnDiscoveredCharacteristics(OnCharacteristicFoundDelegate callback);

        [DllImport("__Internal")]
        private static extern void UnityBLE_registerOnLog(OnLogReceivedDelegate callback);

        [DllImport("__Internal")]
        private static extern void UnityBLE_registerOnWriteCharacteristicCompleted(OnWriteCharacteristicCompletedDelegate callback);

        [DllImport("__Internal")]
        private static extern void UnityBLE_registerOnReadRSSICompleted(OnReadRSSICompletedDelegate callback);


        [DllImport("__Internal")]
        private static extern void UnityBLE_registerOnValueReceived(OnDataReceivedDelegate callback);

        [DllImport("__Internal")]
        private static extern int UnityBLE_discoverServices(string peripheralUUID);

        // Static callback methods (must be static for AOT)
        [MonoPInvokeCallback(typeof(OnPeripheralFoundDelegate))]
        private static void OnDeviceDiscoveredCallback(string deviceJson)
        {
            Debug.Log($"[AppleBleNativePlugin] Device discovered: {deviceJson}");
            var dto = JsonUtility.FromJson<PeripheralDTO>(deviceJson);
            var device = new AppleBlePeripheral(dto);
            _discoveredPeripherals.Add(device);
            BleScanEventDelegates.InvokeDeviceDiscovered(device);
        }

        [MonoPInvokeCallback(typeof(OnConnectedDelegate))]
        private static void OnDeviceConnectedCallback(string deviceJson)
        {
            var dto = PeripheralDTO.FromJson(deviceJson);
            foreach (var p in _discoveredPeripherals)
            {
                if (p.UUID == dto.uuid)
                {
                    BleDeviceEvents.InvokeConnected(p);
                    Debug.Log($"[AppleBleNativePlugin] Device connected: {p.UUID}");
                    return;
                }
            }

            Debug.LogError($"[AppleBleNativePlugin] Device connected: {dto.uuid} but not found in discovered peripherals.");
            DisconnectFromDevice(dto.uuid);
        }

        [MonoPInvokeCallback(typeof(OnDisconnectedDelegate))]
        private static void OnDeviceDisconnectedCallback(string deviceUuid)
        {
            Debug.Log($"[AppleBleNativePlugin] Device disconnected: {deviceUuid}");
            BleDeviceEvents.InvokeDisconnected(deviceUuid);
        }

        [MonoPInvokeCallback(typeof(OnBleErrorDelegate))]
        private static void OnErrorCallback(string errorJson)
        {
            Debug.LogError($"[AppleBleNativePlugin] Error: {errorJson}");
        }

        [MonoPInvokeCallback(typeof(OnLogReceivedDelegate))]
        private static void OnLogCallback(string logMessage)
        {
            var splitIndex = logMessage.IndexOf(':');
            if (splitIndex >= 0)
            {
                string category = logMessage.Substring(0, splitIndex);
                string msg = logMessage.Substring(splitIndex + 1);
                if (category == "Error")
                {
                    Debug.LogError(msg);
                }
                else if (category == "Warning")
                {
                    Debug.LogWarning(msg);
                }
                else
                {
                    Debug.Log(msg);
                }
            }
            else
            {
                Debug.Log(logMessage);
            }
        }

        [MonoPInvokeCallback(typeof(OnBleStatusChangedDelegate))]
        private static void OnBleStatusChangedCallback(int status)
        {
            Debug.Log($"[AppleBleNativePlugin] BLE status changed: {status}");
            BleScanEventDelegates.InvokeBLEStatusChanged((BleStatus)status);
        }

        [MonoPInvokeCallback(typeof(OnClearFoundDevicesDelegate))]
        private static void OnClearFoundDevices()
        {
            Debug.Log("[AppleBleNativePlugin] Found devices cleared");
            foreach (var device in _discoveredPeripherals)
            {
                device.Dispose();
            }
            _discoveredPeripherals.Clear();
            BleScanEventDelegates.InvokeFoundDevicesCleared();
        }

        [MonoPInvokeCallback(typeof(OnServiceFoundDelegate))]
        private static void OnServiceDiscovered(string serviceJson)
        {
            Debug.Log($"[AppleBleNativePlugin] Service found: {serviceJson}");
            ServiceDTO serviceDTO = JsonUtility.FromJson<ServiceDTO>(serviceJson);
            var dto = AppleBleService.FromDTO(serviceDTO);
            BleDeviceEvents.InvokeServicesDiscovered(dto);
        }

        [MonoPInvokeCallback(typeof(OnCharacteristicFoundDelegate))]
        private static void OnCharacteristicDiscovered(string characteristicJson)
        {
            Debug.Log($"[AppleBleNativePlugin] Characteristic found: {characteristicJson}");
            CharacteristicDTO characteristicDTO = JsonUtility.FromJson<CharacteristicDTO>(characteristicJson);
            BleDeviceEvents.InvokeCharacteristicDiscovered(AppleBleCharacteristic.FromDTO(characteristicDTO));
        }

        [MonoPInvokeCallback(typeof(OnWriteCharacteristicCompletedDelegate))]
        private static void OnWriteCharacteristicCompleted(string characteristicUUID, int result, string errorDescription)
        {
            if (result < 0)
            {
                Debug.LogError($"Write operation failed for characteristic {characteristicUUID}: {errorDescription}");
            }
            else
            {
                Debug.Log($"Write operation completed successfully for characteristic {characteristicUUID}");
            }
            BleDeviceEvents.InvokeWriteOperationCompleted(characteristicUUID, result == 0);
        }

        [MonoPInvokeCallback(typeof(OnReadRSSICompletedDelegate))]
        private static void OnReadRSSICompleted(int result, string errorDescription)
        {
            if (result < 0)
            {
                Debug.LogError($"Read RSSI operation failed: {errorDescription}");
            }
            else
            {
                Debug.Log($"Read RSSI operation completed successfully: {result}");
            }
            BleDeviceEvents.InvokeReadRSSICompleted(result);
        }

        [MonoPInvokeCallback(typeof(OnDataReceivedDelegate))]
        private static void OnDataReceived(string peripheralUuid, string data)
        {
            Debug.Log($"[AppleBleNativePlugin] Data received from {peripheralUuid}: {data}");
            var bytes = Convert.FromBase64String(data);
            var encodedStr = Encoding.ASCII.GetString(bytes);
            BleDeviceEvents.InvokeDataReceived(peripheralUuid, encodedStr);
        }

        public static bool IsInitialized { get; private set; } = false;

        private static object _lock = new();

        /// <summary>
        /// Initialize the macOS BLE plugin and register callbacks.
        /// </summary>
        public static void Initialize()
        {
            lock (_lock)
            {
                if (IsInitialized)
                {
                    Debug.Log("[AppleBleNativePlugin] Already initialized");
                    return;
                }
            }

            //Register callbacks first
            UnityBLE_registerOnPeripheralDiscovered(OnDeviceDiscoveredCallback);
            UnityBLE_registerOnPeripheralConnected(OnDeviceConnectedCallback);
            UnityBLE_registerOnPeripheralDisconnected(OnDeviceDisconnectedCallback);
            UnityBLE_registerOnBleErrorDetected(OnErrorCallback);
            UnityBLE_registerOnBleStateChanged(OnBleStatusChangedCallback);
            UnityBLE_registerOnDiscoveredPeripheralCleared(OnClearFoundDevices);
            UnityBLE_registerOnDiscoveredServices(OnServiceDiscovered);
            UnityBLE_registerOnDiscoveredCharacteristics(OnCharacteristicDiscovered);
            UnityBLE_registerOnLog(OnLogCallback);
            UnityBLE_registerOnWriteCharacteristicCompleted(OnWriteCharacteristicCompleted);
            UnityBLE_registerOnReadRSSICompleted(OnReadRSSICompleted);
            UnityBLE_registerOnValueReceived(OnDataReceived);

            Debug.Log("[AppleBleNativePlugin] Initialize completed.");

            lock (_lock)
            {
                IsInitialized = true;
            }
        }

        /// <summary>
        /// Start BLE scanning for the specified duration.
        /// </summary>
        public static void StartScan(ScanFilter filter = null)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("Plugin not initialized. Call Initialize() first.");
            }

            int scanResult = -1;

            if (filter != null)
            {
                if (string.IsNullOrEmpty(filter.Name))
                {
                    filter.Name = null;
                }
                scanResult = UnityBLE_StartScanning(filter.ServiceUuids, filter.Name);
            }
            else
            {
                scanResult = UnityBLE_StartScanning(Array.Empty<string>(), null);
            }

            if (scanResult == 1)
            {
                Debug.Log("[AppleBleNativePlugin] Scanning already started.");
                return;
            }

            if (scanResult == 0)
            {
                return;
            }

            if (scanResult == -1)
            {
                throw new BlePowerTurnedOff();
            }
            else if (scanResult == -2)
            {
                throw new BleUnAuthorized();
            }
            else if (scanResult == -3)
            {
                throw new BleUnsupported();
            }
            else
            {
                throw new SystemException($"Unknown error starting scan: {scanResult}");
            }
        }

        public static bool IsScanning()
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("Plugin not initialized. Call Initialize() first.");
            }

            return UnityBLE_IsScanning();
        }

        /// <summary>
        /// Stop BLE scanning.
        /// </summary>
        public static void StopScan()
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("Plugin not initialized. Call Initialize() first.");
            }

            UnityBLE_StopScanning();
        }

        /// <summary>
        /// Connect to a BLE device by address.
        /// </summary>
        public static void ConnectToDevice(string peripheralUUID)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("Plugin not initialized. Call Initialize() first.");
            }

            var result = UnityBLE_ConnectToPeripheral(peripheralUUID);
            if (result != 0)
            {
                Debug.LogError($"[AppleBleNativePlugin] Failed to connect to device {peripheralUUID}. Error code: {result}");
            }
        }

        /// <summary>
        /// Disconnect from a BLE device by address.
        /// </summary>
        public static void DisconnectFromDevice(string address)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("Plugin not initialized. Call Initialize() first.");
            }

            var result = UnityBLE_DisconnectFromPeripheral(address);
            if (result != 0)
            {
                Debug.LogError($"[AppleBleNativePlugin] Failed to disconnect from device {address}. Error code: {result}");
            }
            else
            {
                Debug.Log($"[AppleBleNativePlugin] Successfully disconnected from device {address}");
            }
        }

        public static int StartDiscoveryService(IBlePeripheral peripheral)
        {
            Debug.Log($"[AppleBleNativePlugin] Starting service discovery for peripheral {peripheral.UUID}");
            if (!IsInitialized)
            {
                throw new InvalidOperationException("Plugin not initialized. Call Initialize() first.");
            }

            if (!peripheral.IsConnected)
            {
                Debug.LogError($"[AppleBleNativePlugin] Cannot discover services for disconnected device {peripheral.UUID}");
                return -1;
            }

            return UnityBLE_discoverServices(peripheral.UUID);
        }

        /// <summary>
        /// Read a characteristic value.
        /// </summary>
        public static void ReadCharacteristic(string address, string serviceUUID, string characteristicUUID)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("Plugin not initialized. Call Initialize() first.");
            }

            var result = UnityBLE_ReadCharacteristic(address, serviceUUID, characteristicUUID);
            if (result != 0)
            {
                Debug.LogError($"Failed to read characteristic {characteristicUUID} from service {serviceUUID} on device {address}. Error code: {result}");
            }
            else
            {
                Debug.Log($"Successfully read characteristic {characteristicUUID} from service {serviceUUID} on device {address}");
            }
        }

        /// <summary>
        /// Write a characteristic value.
        /// </summary>
        public static void WriteCharacteristic(string address, string serviceUUID, string characteristicUUID, byte[] data)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("Plugin not initialized. Call Initialize() first.");
            }

            var result = UnityBLE_WriteCharacteristic(address, serviceUUID, characteristicUUID, data, data?.Length ?? 0);
            if (result != 0)
            {
                Debug.LogError($"Failed to write characteristic {characteristicUUID} to service {serviceUUID} on device {address}. Error code: {result}");
            }
            else
            {
                Debug.Log($"Successfully wrote characteristic {characteristicUUID} to service {serviceUUID} on device {address}");
            }
        }

        /// <summary>
        /// Subscribe to characteristic notifications.
        /// </summary>
        public static void SubscribeCharacteristic(string address, string serviceUUID, string characteristicUUID)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("Plugin not initialized. Call Initialize() first.");
            }

            var result = UnityBLE_SubscribeToCharacteristic(address, serviceUUID, characteristicUUID);
            if (result != 0)
            {
                Debug.LogError($"Failed to subscribe to characteristic {characteristicUUID} on service {serviceUUID} for device {address}. Error code: {result}");
            }
            else
            {
                Debug.Log($"Successfully subscribed to characteristic {characteristicUUID} on service {serviceUUID} for device {address}");
            }
        }

        /// <summary>
        /// Unsubscribe from characteristic notifications.
        /// </summary>
        public static void UnsubscribeCharacteristic(string peripheralUUID, string serviceUUID, string characteristicUUID)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("Plugin not initialized. Call Initialize() first.");
            }

            var result = UnityBLE_UnsubscribeFromCharacteristic(characteristicUUID, serviceUUID, peripheralUUID);
            if (result != 0)
            {
                Debug.LogError($"Failed to unsubscribe from characteristic {characteristicUUID} on service {serviceUUID} for device {peripheralUUID}. Error code: {result}");
            }
            else
            {
                Debug.Log($"Successfully unsubscribed from characteristic {characteristicUUID} on service {serviceUUID} for device {peripheralUUID}");
            }
        }
    }
}

#endif
