using System;
using System.Runtime.InteropServices;
using AOT;
using UnityEngine;

namespace UnityBLE
{
    /// <summary>
    /// Unity bridge for macOS Core Bluetooth native plugin.
    /// </summary>
    public static class MacOSBleNativePlugin
    {
        // Delegates for callbacks from native code
        public delegate void DeviceDiscoveredDelegate(string deviceJson);
        public delegate void DeviceConnectedDelegate(string deviceJson);
        public delegate void DeviceDisconnectedDelegate(string deviceJson);
        public delegate void ScanCompletedDelegate();
        public delegate void CharacteristicValueDelegate(string characteristicJson, string valueHex);
        public delegate void ServicesDiscoveredDelegate(string deviceAddress, string servicesJson);
        public delegate void CharacteristicsDiscoveredDelegate(string deviceAddress, string serviceUuid, string characteristicsJson);
        public delegate void ErrorDelegate(string errorMessage);
        public delegate void LogDelegate(string logMessage);

        // Events that Unity code can subscribe to
        public static event DeviceDiscoveredDelegate OnDeviceDiscovered;
        public static event DeviceConnectedDelegate OnDeviceConnected;
        public static event DeviceDisconnectedDelegate OnDeviceDisconnected;
        public static event ScanCompletedDelegate OnScanCompleted;
        public static event CharacteristicValueDelegate OnCharacteristicValue;
        public static event ErrorDelegate OnError;

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        // Native function imports
        [DllImport("__Internal")]
        private static extern bool MacOSBlePlugin_Initialize();

        [DllImport("__Internal")]
        private static extern bool MacOSBlePlugin_IsBluetoothReady();

        [DllImport("__Internal")]
        private static extern bool MacOSBlePlugin_WaitForBluetoothReady(double timeout);

        [DllImport("__Internal")]
        private static extern bool MacOSBlePlugin_StartScan(double duration);

        [DllImport("__Internal")]
        private static extern void MacOSBlePlugin_StopScan();

        [DllImport("__Internal")]
        private static extern bool MacOSBlePlugin_ConnectToDevice(string address);

        [DllImport("__Internal")]
        private static extern bool MacOSBlePlugin_DisconnectFromDevice(string address);

        [DllImport("__Internal")]
        private static extern IntPtr MacOSBlePlugin_GetServices(string address, out int count);

        [DllImport("__Internal")]
        private static extern IntPtr MacOSBlePlugin_GetCharacteristics(string address, string serviceUUID, out int count);

        [DllImport("__Internal")]
        private static extern IntPtr MacOSBlePlugin_GetCharacteristicProperties(string address, string serviceUUID, string characteristicUUID);

        [DllImport("__Internal")]
        private static extern bool MacOSBlePlugin_ReadCharacteristic(string address, string serviceUUID, string characteristicUUID);

        [DllImport("__Internal")]
        private static extern bool MacOSBlePlugin_WriteCharacteristic(string address, string serviceUUID, string characteristicUUID, string dataHex);

        [DllImport("__Internal")]
        private static extern bool MacOSBlePlugin_SubscribeCharacteristic(string address, string serviceUUID, string characteristicUUID);

        [DllImport("__Internal")]
        private static extern bool MacOSBlePlugin_UnsubscribeCharacteristic(string address, string serviceUUID, string characteristicUUID);

        // Callback registration functions
        [DllImport("__Internal")]
        private static extern void MacOSBlePlugin_SetDeviceDiscoveredCallback(DeviceDiscoveredDelegate callback);

        [DllImport("__Internal")]
        private static extern void MacOSBlePlugin_SetDeviceConnectedCallback(DeviceConnectedDelegate callback);

        [DllImport("__Internal")]
        private static extern void MacOSBlePlugin_SetDeviceDisconnectedCallback(DeviceDisconnectedDelegate callback);

        [DllImport("__Internal")]
        private static extern void MacOSBlePlugin_SetScanCompletedCallback(ScanCompletedDelegate callback);

        [DllImport("__Internal")]
        private static extern void MacOSBlePlugin_SetCharacteristicValueCallback(CharacteristicValueDelegate callback);

        [DllImport("__Internal")]
        private static extern void MacOSBlePlugin_SetErrorCallback(ErrorDelegate callback);

        [DllImport("__Internal")]
        private static extern void MacOSBlePlugin_SetLogCallback(LogDelegate callback);

        [DllImport("__Internal")]
        private static extern void MacOSBlePlugin_SetServicesDiscoveredCallback(ServicesDiscoveredDelegate callback);

        [DllImport("__Internal")]
        private static extern void MacOSBlePlugin_SetCharacteristicsDiscoveredCallback(CharacteristicsDiscoveredDelegate callback);

        // Static callback methods (must be static for AOT)
        [MonoPInvokeCallback(typeof(DeviceDiscoveredDelegate))]
        private static void OnDeviceDiscoveredCallback(string deviceJson)
        {
            Debug.Log($"[MacOSBleNativePlugin] Device discovered: {deviceJson}");
            OnDeviceDiscovered?.Invoke(deviceJson);
        }

        [MonoPInvokeCallback(typeof(DeviceConnectedDelegate))]
        private static void OnDeviceConnectedCallback(string deviceJson)
        {
            Debug.Log($"[MacOSBleNativePlugin] Device connected: {deviceJson}");
            OnDeviceConnected?.Invoke(deviceJson);
        }

        [MonoPInvokeCallback(typeof(DeviceDisconnectedDelegate))]
        private static void OnDeviceDisconnectedCallback(string deviceJson)
        {
            Debug.Log($"[MacOSBleNativePlugin] Device disconnected: {deviceJson}");
            OnDeviceDisconnected?.Invoke(deviceJson);
        }

        [MonoPInvokeCallback(typeof(ScanCompletedDelegate))]
        private static void OnScanCompletedCallback()
        {
            Debug.Log("[MacOSBleNativePlugin] Scan completed");
            OnScanCompleted?.Invoke();
        }

        [MonoPInvokeCallback(typeof(CharacteristicValueDelegate))]
        private static void OnCharacteristicValueCallback(string characteristicJson, string valueHex)
        {
            Debug.Log($"[MacOSBleNativePlugin] Characteristic value: {characteristicJson} = {valueHex}");
            OnCharacteristicValue?.Invoke(characteristicJson, valueHex);
        }

        [MonoPInvokeCallback(typeof(ErrorDelegate))]
        private static void OnErrorCallback(string errorMessage)
        {
            Debug.LogError($"[MacOSBleNativePlugin] Error: {errorMessage}");
            OnError?.Invoke(errorMessage);
        }

        [MonoPInvokeCallback(typeof(LogDelegate))]
        private static void OnLogCallback(string logMessage)
        {
            Debug.Log(logMessage);
        }

        private static bool _isInitialized = false;

        /// <summary>
        /// Initialize the macOS BLE plugin and register callbacks.
        /// </summary>
        public static bool Initialize()
        {
            if (_isInitialized)
            {
                Debug.Log("[MacOSBleNativePlugin] Already initialized");
                return true;
            }

            try
            {
                // Register callbacks first
                MacOSBlePlugin_SetDeviceDiscoveredCallback(OnDeviceDiscoveredCallback);
                MacOSBlePlugin_SetDeviceConnectedCallback(OnDeviceConnectedCallback);
                MacOSBlePlugin_SetDeviceDisconnectedCallback(OnDeviceDisconnectedCallback);
                MacOSBlePlugin_SetScanCompletedCallback(OnScanCompletedCallback);
                MacOSBlePlugin_SetCharacteristicValueCallback(OnCharacteristicValueCallback);
                MacOSBlePlugin_SetErrorCallback(OnErrorCallback);
                MacOSBlePlugin_SetLogCallback(OnLogCallback);

                // Initialize the native plugin
                bool result = MacOSBlePlugin_Initialize();
                if (result)
                {
                    _isInitialized = true;
                    Debug.Log("[MacOSBleNativePlugin] Successfully initialized");

                    // Wait for Bluetooth to be ready (up to 5 seconds)
                    Debug.Log("[MacOSBleNativePlugin] Waiting for Bluetooth to be ready...");
                    bool bluetoothReady = MacOSBlePlugin_WaitForBluetoothReady(5.0);
                    if (bluetoothReady)
                    {
                        Debug.Log("[MacOSBleNativePlugin] Bluetooth is ready");
                    }
                    else
                    {
                        Debug.LogWarning("[MacOSBleNativePlugin] Bluetooth not ready after timeout");
                    }
                }
                else
                {
                    Debug.LogError("[MacOSBleNativePlugin] Failed to initialize");
                }
                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MacOSBleNativePlugin] Exception during initialization: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Check if Bluetooth is ready for use.
        /// </summary>
        public static bool IsBluetoothReady()
        {
            if (!_isInitialized)
            {
                return false;
            }

            try
            {
                return MacOSBlePlugin_IsBluetoothReady();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MacOSBleNativePlugin] Exception during IsBluetoothReady: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Wait for Bluetooth to be ready with timeout.
        /// </summary>
        public static bool WaitForBluetoothReady(double timeout)
        {
            if (!_isInitialized)
            {
                return false;
            }

            try
            {
                return MacOSBlePlugin_WaitForBluetoothReady(timeout);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MacOSBleNativePlugin] Exception during WaitForBluetoothReady: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Start BLE scanning for the specified duration.
        /// </summary>
        public static bool StartScan(double duration)
        {
            if (!_isInitialized)
            {
                Debug.LogError("[MacOSBleNativePlugin] Not initialized");
                return false;
            }

            if (!IsBluetoothReady())
            {
                Debug.LogError("[MacOSBleNativePlugin] Bluetooth is not ready. Please wait a moment and try again.");
                return false;
            }

            try
            {
                return MacOSBlePlugin_StartScan(duration);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MacOSBleNativePlugin] Exception during StartScan: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Stop BLE scanning.
        /// </summary>
        public static void StopScan()
        {
            if (!_isInitialized)
            {
                Debug.LogError("[MacOSBleNativePlugin] Not initialized");
                return;
            }

            try
            {
                MacOSBlePlugin_StopScan();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MacOSBleNativePlugin] Exception during StopScan: {ex.Message}");
            }
        }

        /// <summary>
        /// Connect to a BLE device by address.
        /// </summary>
        public static bool ConnectToDevice(string address)
        {
            if (!_isInitialized)
            {
                Debug.LogError("[MacOSBleNativePlugin] Not initialized");
                return false;
            }

            try
            {
                return MacOSBlePlugin_ConnectToDevice(address);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MacOSBleNativePlugin] Exception during ConnectToDevice: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Disconnect from a BLE device by address.
        /// </summary>
        public static bool DisconnectFromDevice(string address)
        {
            if (!_isInitialized)
            {
                Debug.LogError("[MacOSBleNativePlugin] Not initialized");
                return false;
            }

            try
            {
                return MacOSBlePlugin_DisconnectFromDevice(address);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MacOSBleNativePlugin] Exception during DisconnectFromDevice: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get services for a connected device.
        /// </summary>
        public static string[] GetServices(string address)
        {
            if (!_isInitialized)
            {
                Debug.LogError("[MacOSBleNativePlugin] Not initialized");
                return new string[0];
            }

            try
            {
                int count;
                IntPtr servicesPtr = MacOSBlePlugin_GetServices(address, out count);

                if (servicesPtr == IntPtr.Zero || count == 0)
                {
                    return new string[0];
                }

                string[] services = new string[count];
                for (int i = 0; i < count; i++)
                {
                    IntPtr stringPtr = Marshal.ReadIntPtr(servicesPtr, i * IntPtr.Size);
                    services[i] = Marshal.PtrToStringAnsi(stringPtr);
                    // Free the individual string
                    Marshal.FreeHGlobal(stringPtr);
                }

                // Free the array
                Marshal.FreeHGlobal(servicesPtr);
                return services;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MacOSBleNativePlugin] Exception during GetServices: {ex.Message}");
                return new string[0];
            }
        }

        /// <summary>
        /// Get characteristics for a service.
        /// </summary>
        public static string[] GetCharacteristics(string address, string serviceUUID)
        {
            if (!_isInitialized)
            {
                Debug.LogError("[MacOSBleNativePlugin] Not initialized");
                return new string[0];
            }

            try
            {
                int count;
                IntPtr characteristicsPtr = MacOSBlePlugin_GetCharacteristics(address, serviceUUID, out count);

                if (characteristicsPtr == IntPtr.Zero || count == 0)
                {
                    return new string[0];
                }

                string[] characteristics = new string[count];
                for (int i = 0; i < count; i++)
                {
                    IntPtr stringPtr = Marshal.ReadIntPtr(characteristicsPtr, i * IntPtr.Size);
                    characteristics[i] = Marshal.PtrToStringAnsi(stringPtr);
                    // Free the individual string
                    Marshal.FreeHGlobal(stringPtr);
                }

                // Free the array
                Marshal.FreeHGlobal(characteristicsPtr);
                return characteristics;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MacOSBleNativePlugin] Exception during GetCharacteristics: {ex.Message}");
                return new string[0];
            }
        }

        /// <summary>
        /// Get characteristic properties.
        /// </summary>
        public static CharacteristicProperties GetCharacteristicProperties(string address, string serviceUUID, string characteristicUUID)
        {
            if (!_isInitialized)
            {
                Debug.LogError("[MacOSBleNativePlugin] Not initialized");
                return CharacteristicProperties.None;
            }

            try
            {
                IntPtr propertiesPtr = MacOSBlePlugin_GetCharacteristicProperties(address, serviceUUID, characteristicUUID);

                if (propertiesPtr == IntPtr.Zero)
                {
                    Debug.LogWarning($"[MacOSBleNativePlugin] Failed to get properties for characteristic {characteristicUUID}");
                    return CharacteristicProperties.None;
                }

                string propertiesJson = Marshal.PtrToStringAnsi(propertiesPtr);
                Marshal.FreeHGlobal(propertiesPtr);

                // Parse properties from JSON (assuming native plugin returns properties as integer)
                if (int.TryParse(propertiesJson, out int propertiesValue))
                {
                    return (CharacteristicProperties)propertiesValue;
                }

                Debug.LogWarning($"[MacOSBleNativePlugin] Failed to parse characteristic properties: {propertiesJson}");
                return CharacteristicProperties.None;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MacOSBleNativePlugin] Exception during GetCharacteristicProperties: {ex.Message}");
                return CharacteristicProperties.None;
            }
        }

        /// <summary>
        /// Read a characteristic value.
        /// </summary>
        public static bool ReadCharacteristic(string address, string serviceUUID, string characteristicUUID)
        {
            if (!_isInitialized)
            {
                Debug.LogError("[MacOSBleNativePlugin] Not initialized");
                return false;
            }

            try
            {
                return MacOSBlePlugin_ReadCharacteristic(address, serviceUUID, characteristicUUID);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MacOSBleNativePlugin] Exception during ReadCharacteristic: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Write a characteristic value.
        /// </summary>
        public static bool WriteCharacteristic(string address, string serviceUUID, string characteristicUUID, byte[] data)
        {
            if (!_isInitialized)
            {
                Debug.LogError("[MacOSBleNativePlugin] Not initialized");
                return false;
            }

            try
            {
                // Convert byte array to hex string
                string dataHex = "";
                if (data != null && data.Length > 0)
                {
                    dataHex = BitConverter.ToString(data).Replace("-", "").ToLower();
                }

                return MacOSBlePlugin_WriteCharacteristic(address, serviceUUID, characteristicUUID, dataHex);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MacOSBleNativePlugin] Exception during WriteCharacteristic: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Subscribe to characteristic notifications.
        /// </summary>
        public static bool SubscribeCharacteristic(string address, string serviceUUID, string characteristicUUID)
        {
            if (!_isInitialized)
            {
                Debug.LogError("[MacOSBleNativePlugin] Not initialized");
                return false;
            }

            try
            {
                return MacOSBlePlugin_SubscribeCharacteristic(address, serviceUUID, characteristicUUID);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MacOSBleNativePlugin] Exception during SubscribeCharacteristic: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Unsubscribe from characteristic notifications.
        /// </summary>
        public static bool UnsubscribeCharacteristic(string address, string serviceUUID, string characteristicUUID)
        {
            if (!_isInitialized)
            {
                Debug.LogError("[MacOSBleNativePlugin] Not initialized");
                return false;
            }

            try
            {
                return MacOSBlePlugin_UnsubscribeCharacteristic(address, serviceUUID, characteristicUUID);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MacOSBleNativePlugin] Exception during UnsubscribeCharacteristic: {ex.Message}");
                return false;
            }
        }
#else
        // Stub implementations for non-macOS platforms
        public static bool Initialize()
        {
            Debug.LogWarning("[MacOSBleNativePlugin] Not available on this platform");
            return false;
        }

        public static bool IsBluetoothReady()
        {
            Debug.LogWarning("[MacOSBleNativePlugin] Not available on this platform");
            return false;
        }

        public static bool WaitForBluetoothReady(double timeout)
        {
            Debug.LogWarning("[MacOSBleNativePlugin] Not available on this platform");
            return false;
        }

        public static bool StartScan(double duration)
        {
            Debug.LogWarning("[MacOSBleNativePlugin] Not available on this platform");
            return false;
        }

        public static void StopScan()
        {
            Debug.LogWarning("[MacOSBleNativePlugin] Not available on this platform");
        }

        public static bool ConnectToDevice(string address)
        {
            Debug.LogWarning("[MacOSBleNativePlugin] Not available on this platform");
            return false;
        }

        public static bool DisconnectFromDevice(string address)
        {
            Debug.LogWarning("[MacOSBleNativePlugin] Not available on this platform");
            return false;
        }

        public static string[] GetServices(string address)
        {
            Debug.LogWarning("[MacOSBleNativePlugin] Not available on this platform");
            return new string[0];
        }

        public static string[] GetCharacteristics(string address, string serviceUUID)
        {
            Debug.LogWarning("[MacOSBleNativePlugin] Not available on this platform");
            return new string[0];
        }

        public static CharacteristicProperties GetCharacteristicProperties(string address, string serviceUUID, string characteristicUUID)
        {
            Debug.LogWarning("[MacOSBleNativePlugin] Not available on this platform");
            return CharacteristicProperties.None;
        }

        public static bool ReadCharacteristic(string address, string serviceUUID, string characteristicUUID)
        {
            Debug.LogWarning("[MacOSBleNativePlugin] Not available on this platform");
            return false;
        }

        public static bool WriteCharacteristic(string address, string serviceUUID, string characteristicUUID, byte[] data)
        {
            Debug.LogWarning("[MacOSBleNativePlugin] Not available on this platform");
            return false;
        }

        public static bool SubscribeCharacteristic(string address, string serviceUUID, string characteristicUUID)
        {
            Debug.LogWarning("[MacOSBleNativePlugin] Not available on this platform");
            return false;
        }

        public static bool UnsubscribeCharacteristic(string address, string serviceUUID, string characteristicUUID)
        {
            Debug.LogWarning("[MacOSBleNativePlugin] Not available on this platform");
            return false;
        }

#endif
    }
}