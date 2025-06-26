using System;
using System.Runtime.InteropServices;
using AOT;
using UnityEngine;

namespace UnityBLE.iOS
{
    /// <summary>
    /// Unity bridge for iOS Core Bluetooth native plugin.
    /// </summary>
    public static class iOSBleNativePlugin
    {
        // Delegates for callbacks from native code
        public delegate void DeviceDiscoveredDelegate(string deviceJson);
        public delegate void DeviceConnectedDelegate(string deviceJson);
        public delegate void DeviceDisconnectedDelegate(string deviceJson);
        public delegate void ScanCompletedDelegate();
        public delegate void CharacteristicValueDelegate(string characteristicJson, string valueHex);
        public delegate void ErrorDelegate(string errorMessage);
        public delegate void LogDelegate(string logMessage);

        // Events that Unity code can subscribe to
        public static event DeviceDiscoveredDelegate OnDeviceDiscovered;
        public static event DeviceConnectedDelegate OnDeviceConnected;
        public static event DeviceDisconnectedDelegate OnDeviceDisconnected;
        public static event ScanCompletedDelegate OnScanCompleted;
        public static event CharacteristicValueDelegate OnCharacteristicValue;
        public static event ErrorDelegate OnError;

#if UNITY_IOS && !UNITY_EDITOR
        // Native function imports
        [DllImport("__Internal")]
        private static extern bool iOSBlePlugin_Initialize();

        [DllImport("__Internal")]
        private static extern bool iOSBlePlugin_IsBluetoothReady();

        [DllImport("__Internal")]
        private static extern bool iOSBlePlugin_WaitForBluetoothReady(double timeout);

        [DllImport("__Internal")]
        private static extern bool iOSBlePlugin_StartScan(double duration);

        [DllImport("__Internal")]
        private static extern void iOSBlePlugin_StopScan();

        [DllImport("__Internal")]
        private static extern bool iOSBlePlugin_ConnectToDevice(string address);

        [DllImport("__Internal")]
        private static extern bool iOSBlePlugin_DisconnectFromDevice(string address);

        [DllImport("__Internal")]
        private static extern IntPtr iOSBlePlugin_GetServices(string address, out int count);

        [DllImport("__Internal")]
        private static extern IntPtr iOSBlePlugin_GetCharacteristics(string address, string serviceUUID, out int count);

        [DllImport("__Internal")]
        private static extern bool iOSBlePlugin_ReadCharacteristic(string address, string serviceUUID, string characteristicUUID);

        [DllImport("__Internal")]
        private static extern bool iOSBlePlugin_WriteCharacteristic(string address, string serviceUUID, string characteristicUUID, string dataHex);

        // Callback registration functions
        [DllImport("__Internal")]
        private static extern void iOSBlePlugin_SetDeviceDiscoveredCallback(DeviceDiscoveredDelegate callback);

        [DllImport("__Internal")]
        private static extern void iOSBlePlugin_SetDeviceConnectedCallback(DeviceConnectedDelegate callback);

        [DllImport("__Internal")]
        private static extern void iOSBlePlugin_SetDeviceDisconnectedCallback(DeviceDisconnectedDelegate callback);

        [DllImport("__Internal")]
        private static extern void iOSBlePlugin_SetScanCompletedCallback(ScanCompletedDelegate callback);

        [DllImport("__Internal")]
        private static extern void iOSBlePlugin_SetCharacteristicValueCallback(CharacteristicValueDelegate callback);

        [DllImport("__Internal")]
        private static extern void iOSBlePlugin_SetErrorCallback(ErrorDelegate callback);

        [DllImport("__Internal")]
        private static extern void iOSBlePlugin_SetLogCallback(LogDelegate callback);

        // Static callback methods (must be static for AOT)
        [MonoPInvokeCallback(typeof(DeviceDiscoveredDelegate))]
        private static void OnDeviceDiscoveredCallback(string deviceJson)
        {
            Debug.Log($"[iOSBleNativePlugin] Device discovered: {deviceJson}");
            OnDeviceDiscovered?.Invoke(deviceJson);
        }

        [MonoPInvokeCallback(typeof(DeviceConnectedDelegate))]
        private static void OnDeviceConnectedCallback(string deviceJson)
        {
            Debug.Log($"[iOSBleNativePlugin] Device connected: {deviceJson}");
            OnDeviceConnected?.Invoke(deviceJson);
        }

        [MonoPInvokeCallback(typeof(DeviceDisconnectedDelegate))]
        private static void OnDeviceDisconnectedCallback(string deviceJson)
        {
            Debug.Log($"[iOSBleNativePlugin] Device disconnected: {deviceJson}");
            OnDeviceDisconnected?.Invoke(deviceJson);
        }

        [MonoPInvokeCallback(typeof(ScanCompletedDelegate))]
        private static void OnScanCompletedCallback()
        {
            Debug.Log("[iOSBleNativePlugin] Scan completed");
            OnScanCompleted?.Invoke();
        }

        [MonoPInvokeCallback(typeof(CharacteristicValueDelegate))]
        private static void OnCharacteristicValueCallback(string characteristicJson, string valueHex)
        {
            Debug.Log($"[iOSBleNativePlugin] Characteristic value: {characteristicJson} = {valueHex}");
            OnCharacteristicValue?.Invoke(characteristicJson, valueHex);
        }

        [MonoPInvokeCallback(typeof(ErrorDelegate))]
        private static void OnErrorCallback(string errorMessage)
        {
            Debug.LogError($"[iOSBleNativePlugin] Error: {errorMessage}");
            OnError?.Invoke(errorMessage);
        }

        [MonoPInvokeCallback(typeof(LogDelegate))]
        private static void OnLogCallback(string logMessage)
        {
            Debug.Log(logMessage);
        }

        private static bool _isInitialized = false;

        /// <summary>
        /// Initialize the iOS BLE plugin and register callbacks.
        /// </summary>
        public static bool Initialize()
        {
            if (_isInitialized)
            {
                Debug.Log("[iOSBleNativePlugin] Already initialized");
                return true;
            }

            try
            {
                // Register callbacks first
                iOSBlePlugin_SetDeviceDiscoveredCallback(OnDeviceDiscoveredCallback);
                iOSBlePlugin_SetDeviceConnectedCallback(OnDeviceConnectedCallback);
                iOSBlePlugin_SetDeviceDisconnectedCallback(OnDeviceDisconnectedCallback);
                iOSBlePlugin_SetScanCompletedCallback(OnScanCompletedCallback);
                iOSBlePlugin_SetCharacteristicValueCallback(OnCharacteristicValueCallback);
                iOSBlePlugin_SetErrorCallback(OnErrorCallback);
                iOSBlePlugin_SetLogCallback(OnLogCallback);

                // Initialize the native plugin
                bool result = iOSBlePlugin_Initialize();
                if (result)
                {
                    _isInitialized = true;
                    Debug.Log("[iOSBleNativePlugin] Successfully initialized");
                    
                    // Wait for Bluetooth to be ready (up to 5 seconds)
                    Debug.Log("[iOSBleNativePlugin] Waiting for Bluetooth to be ready...");
                    bool bluetoothReady = iOSBlePlugin_WaitForBluetoothReady(5.0);
                    if (bluetoothReady)
                    {
                        Debug.Log("[iOSBleNativePlugin] Bluetooth is ready");
                    }
                    else
                    {
                        Debug.LogWarning("[iOSBleNativePlugin] Bluetooth not ready after timeout");
                    }
                }
                else
                {
                    Debug.LogError("[iOSBleNativePlugin] Failed to initialize");
                }
                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[iOSBleNativePlugin] Exception during initialization: {ex.Message}");
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
                return iOSBlePlugin_IsBluetoothReady();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[iOSBleNativePlugin] Exception during IsBluetoothReady: {ex.Message}");
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
                return iOSBlePlugin_WaitForBluetoothReady(timeout);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[iOSBleNativePlugin] Exception during WaitForBluetoothReady: {ex.Message}");
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
                Debug.LogError("[iOSBleNativePlugin] Not initialized");
                return false;
            }

            if (!IsBluetoothReady())
            {
                Debug.LogError("[iOSBleNativePlugin] Bluetooth is not ready. Please wait a moment and try again.");
                return false;
            }

            try
            {
                return iOSBlePlugin_StartScan(duration);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[iOSBleNativePlugin] Exception during StartScan: {ex.Message}");
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
                Debug.LogError("[iOSBleNativePlugin] Not initialized");
                return;
            }

            try
            {
                iOSBlePlugin_StopScan();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[iOSBleNativePlugin] Exception during StopScan: {ex.Message}");
            }
        }

        /// <summary>
        /// Connect to a BLE device by address.
        /// </summary>
        public static bool ConnectToDevice(string address)
        {
            if (!_isInitialized)
            {
                Debug.LogError("[iOSBleNativePlugin] Not initialized");
                return false;
            }

            try
            {
                return iOSBlePlugin_ConnectToDevice(address);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[iOSBleNativePlugin] Exception during ConnectToDevice: {ex.Message}");
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
                Debug.LogError("[iOSBleNativePlugin] Not initialized");
                return false;
            }

            try
            {
                return iOSBlePlugin_DisconnectFromDevice(address);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[iOSBleNativePlugin] Exception during DisconnectFromDevice: {ex.Message}");
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
                Debug.LogError("[iOSBleNativePlugin] Not initialized");
                return new string[0];
            }

            try
            {
                int count;
                IntPtr servicesPtr = iOSBlePlugin_GetServices(address, out count);

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
                Debug.LogError($"[iOSBleNativePlugin] Exception during GetServices: {ex.Message}");
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
                Debug.LogError("[iOSBleNativePlugin] Not initialized");
                return new string[0];
            }

            try
            {
                int count;
                IntPtr characteristicsPtr = iOSBlePlugin_GetCharacteristics(address, serviceUUID, out count);

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
                Debug.LogError($"[iOSBleNativePlugin] Exception during GetCharacteristics: {ex.Message}");
                return new string[0];
            }
        }

        /// <summary>
        /// Read a characteristic value.
        /// </summary>
        public static bool ReadCharacteristic(string address, string serviceUUID, string characteristicUUID)
        {
            if (!_isInitialized)
            {
                Debug.LogError("[iOSBleNativePlugin] Not initialized");
                return false;
            }

            try
            {
                return iOSBlePlugin_ReadCharacteristic(address, serviceUUID, characteristicUUID);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[iOSBleNativePlugin] Exception during ReadCharacteristic: {ex.Message}");
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
                Debug.LogError("[iOSBleNativePlugin] Not initialized");
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

                return iOSBlePlugin_WriteCharacteristic(address, serviceUUID, characteristicUUID, dataHex);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[iOSBleNativePlugin] Exception during WriteCharacteristic: {ex.Message}");
                return false;
            }
        }
#else
        // Stub implementations for non-iOS platforms
        public static bool Initialize()
        {
            Debug.LogWarning("[iOSBleNativePlugin] Not available on this platform");
            return false;
        }

        public static bool IsBluetoothReady()
        {
            Debug.LogWarning("[iOSBleNativePlugin] Not available on this platform");
            return false;
        }
        
        public static bool WaitForBluetoothReady(double timeout)
        {
            Debug.LogWarning("[iOSBleNativePlugin] Not available on this platform");
            return false;
        }

        public static bool StartScan(double duration)
        {
            Debug.LogWarning("[iOSBleNativePlugin] Not available on this platform");
            return false;
        }

        public static void StopScan()
        {
            Debug.LogWarning("[iOSBleNativePlugin] Not available on this platform");
        }

        public static bool ConnectToDevice(string address)
        {
            Debug.LogWarning("[iOSBleNativePlugin] Not available on this platform");
            return false;
        }

        public static bool DisconnectFromDevice(string address)
        {
            Debug.LogWarning("[iOSBleNativePlugin] Not available on this platform");
            return false;
        }

        public static string[] GetServices(string address)
        {
            Debug.LogWarning("[iOSBleNativePlugin] Not available on this platform");
            return new string[0];
        }

        public static string[] GetCharacteristics(string address, string serviceUUID)
        {
            Debug.LogWarning("[iOSBleNativePlugin] Not available on this platform");
            return new string[0];
        }

        public static bool ReadCharacteristic(string address, string serviceUUID, string characteristicUUID)
        {
            Debug.LogWarning("[iOSBleNativePlugin] Not available on this platform");
            return false;
        }

        public static bool WriteCharacteristic(string address, string serviceUUID, string characteristicUUID, byte[] data)
        {
            Debug.LogWarning("[iOSBleNativePlugin] Not available on this platform");
            return false;
        }
#endif
    }
}