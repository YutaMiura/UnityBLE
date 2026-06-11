using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using AOT;
using UnityBLE.windows;
using UnityEngine;

namespace UnityBLE
{
    /// <summary>
    /// Unity bridge for the Windows (C++/WinRT) BLE native plugin.
    /// Mirrors the contract of <see cref="AppleBleNativePlugin"/> so the rest of
    /// the package can be platform agnostic.
    /// </summary>
    public static class WindowsBleNativePlugin
    {
        // Delegates for callbacks from native code
        public delegate void OnPeripheralFoundDelegate(string deviceJson);
        public delegate void OnConnectedDelegate(string peripheralJson);
        public delegate void OnDisconnectedDelegate(string peripheralUUID);
        public delegate void OnBleStatusChangedDelegate(int status);
        public delegate void OnBleErrorDelegate(string errorJson);
        public delegate void OnClearFoundDevicesDelegate();

        public delegate void OnServiceFoundDelegate(string serviceJson);
        public delegate void OnCharacteristicFoundDelegate(string characteristicJson);

        public delegate void OnLogReceivedDelegate(string logJson);

        public delegate void OnWriteCharacteristicCompletedDelegate(string characteristicUUID, int result, string errorDescription);
        public delegate void OnReadRSSICompletedDelegate(int result, string errorDescription);

        // First argument carries the characteristic UUID (not the peripheral UUID),
        // and data is Base64 encoded. This matches how the read/subscribe commands
        // route results through BleDeviceEvents.OnDataReceived keyed by characteristic.
        public delegate void OnDataReceivedDelegate(string characteristicUuid, string base64Data);

        private static readonly List<IBlePeripheral> _discoveredPeripherals = new List<IBlePeripheral>();

        private const string PluginName = "UnityBleWindows";
        private const string PluginEntryPointPrefix = "UnityBLEWin_";

        // Native function imports
        [DllImport(PluginName, EntryPoint = PluginEntryPointPrefix + "StartScanning", CharSet = CharSet.Ansi)]
        private static extern int UnityBLE_StartScanning(string serviceUuidsCsv, string nameFilter);

        [DllImport(PluginName, EntryPoint = PluginEntryPointPrefix + "StopScanning")]
        private static extern void UnityBLE_StopScanning();

        [DllImport(PluginName, EntryPoint = PluginEntryPointPrefix + "IsScanning")]
        private static extern int UnityBLE_IsScanning();

        [DllImport(PluginName, EntryPoint = PluginEntryPointPrefix + "GetState")]
        private static extern int UnityBLE_GetState();

        [DllImport(PluginName, EntryPoint = PluginEntryPointPrefix + "ConnectToPeripheral", CharSet = CharSet.Ansi)]
        private static extern int UnityBLE_ConnectToPeripheral(string peripheralUUID);

        [DllImport(PluginName, EntryPoint = PluginEntryPointPrefix + "DisconnectFromPeripheral", CharSet = CharSet.Ansi)]
        private static extern int UnityBLE_DisconnectFromPeripheral(string peripheralUUID);

        [DllImport(PluginName, EntryPoint = PluginEntryPointPrefix + "ReadCharacteristic", CharSet = CharSet.Ansi)]
        private static extern int UnityBLE_ReadCharacteristic(string peripheralUUID, string serviceUUID, string characteristicUUID);

        [DllImport(PluginName, EntryPoint = PluginEntryPointPrefix + "WriteCharacteristic", CharSet = CharSet.Ansi)]
        private static extern int UnityBLE_WriteCharacteristic(string peripheralUUID, string serviceUUID, string characteristicUUID, byte[] data, int length, int withResponse);

        [DllImport(PluginName, EntryPoint = PluginEntryPointPrefix + "SubscribeToCharacteristic", CharSet = CharSet.Ansi)]
        private static extern int UnityBLE_SubscribeToCharacteristic(string peripheralUUID, string serviceUUID, string characteristicUUID);

        [DllImport(PluginName, EntryPoint = PluginEntryPointPrefix + "UnsubscribeFromCharacteristic", CharSet = CharSet.Ansi)]
        private static extern int UnityBLE_UnsubscribeFromCharacteristic(string peripheralUUID, string serviceUUID, string characteristicUUID);

        [DllImport(PluginName, EntryPoint = PluginEntryPointPrefix + "DiscoverServices", CharSet = CharSet.Ansi)]
        private static extern int UnityBLE_DiscoverServices(string peripheralUUID);

        [DllImport(PluginName, EntryPoint = PluginEntryPointPrefix + "registerOnPeripheralDiscovered")]
        private static extern void UnityBLE_registerOnPeripheralDiscovered(OnPeripheralFoundDelegate callback);

        [DllImport(PluginName, EntryPoint = PluginEntryPointPrefix + "registerOnPeripheralConnected")]
        private static extern void UnityBLE_registerOnPeripheralConnected(OnConnectedDelegate callback);

        [DllImport(PluginName, EntryPoint = PluginEntryPointPrefix + "registerOnPeripheralDisconnected")]
        private static extern void UnityBLE_registerOnPeripheralDisconnected(OnDisconnectedDelegate callback);

        [DllImport(PluginName, EntryPoint = PluginEntryPointPrefix + "registerOnBleErrorDetected")]
        private static extern void UnityBLE_registerOnBleErrorDetected(OnBleErrorDelegate callback);

        [DllImport(PluginName, EntryPoint = PluginEntryPointPrefix + "registerOnBleStateChanged")]
        private static extern void UnityBLE_registerOnBleStateChanged(OnBleStatusChangedDelegate callback);

        [DllImport(PluginName, EntryPoint = PluginEntryPointPrefix + "registerOnDiscoveredPeripheralCleared")]
        private static extern void UnityBLE_registerOnDiscoveredPeripheralCleared(OnClearFoundDevicesDelegate callback);

        [DllImport(PluginName, EntryPoint = PluginEntryPointPrefix + "registerOnDiscoveredServices")]
        private static extern void UnityBLE_registerOnDiscoveredServices(OnServiceFoundDelegate callback);

        [DllImport(PluginName, EntryPoint = PluginEntryPointPrefix + "registerOnDiscoveredCharacteristics")]
        private static extern void UnityBLE_registerOnDiscoveredCharacteristics(OnCharacteristicFoundDelegate callback);

        [DllImport(PluginName, EntryPoint = PluginEntryPointPrefix + "registerOnLog")]
        private static extern void UnityBLE_registerOnLog(OnLogReceivedDelegate callback);

        [DllImport(PluginName, EntryPoint = PluginEntryPointPrefix + "registerOnWriteCharacteristicCompleted")]
        private static extern void UnityBLE_registerOnWriteCharacteristicCompleted(OnWriteCharacteristicCompletedDelegate callback);

        [DllImport(PluginName, EntryPoint = PluginEntryPointPrefix + "registerOnReadRSSICompleted")]
        private static extern void UnityBLE_registerOnReadRSSICompleted(OnReadRSSICompletedDelegate callback);

        [DllImport(PluginName, EntryPoint = PluginEntryPointPrefix + "registerOnValueReceived")]
        private static extern void UnityBLE_registerOnValueReceived(OnDataReceivedDelegate callback);

        // Unity main-thread SynchronizationContext, captured in Initialize().
        // WinRT delivers BLE events on thread-pool threads; all callbacks are
        // marshaled back to the main thread so that TaskCompletionSource
        // continuations (and downstream UniTask/Unity APIs) run there.
        private static SynchronizationContext _mainContext;

        // Keep strong references to the delegates handed to native code. Without
        // these, the GC can collect the delegate instances after Initialize()
        // returns, and a later native callback (e.g. a notification) would invoke
        // a freed function pointer. In the Editor (Mono) [MonoPInvokeCallback]
        // does not root them, so this is required for notifications to work.
        private static readonly OnPeripheralFoundDelegate _cbPeripheralFound = OnDeviceDiscoveredCallback;
        private static readonly OnConnectedDelegate _cbConnected = OnDeviceConnectedCallback;
        private static readonly OnDisconnectedDelegate _cbDisconnected = OnDeviceDisconnectedCallback;
        private static readonly OnBleErrorDelegate _cbError = OnErrorCallback;
        private static readonly OnBleStatusChangedDelegate _cbStateChanged = OnBleStatusChangedCallback;
        private static readonly OnClearFoundDevicesDelegate _cbCleared = OnClearFoundDevices;
        private static readonly OnServiceFoundDelegate _cbService = OnServiceDiscovered;
        private static readonly OnCharacteristicFoundDelegate _cbCharacteristic = OnCharacteristicDiscovered;
        private static readonly OnLogReceivedDelegate _cbLog = OnLogCallback;
        private static readonly OnWriteCharacteristicCompletedDelegate _cbWriteCompleted = OnWriteCharacteristicCompleted;
        private static readonly OnReadRSSICompletedDelegate _cbReadRssi = OnReadRSSICompleted;
        private static readonly OnDataReceivedDelegate _cbValueReceived = OnDataReceived;

        private static void Dispatch(Action action)
        {
            var ctx = _mainContext;
            if (ctx != null)
            {
                ctx.Post(_ =>
                {
                    try { action(); }
                    catch (Exception ex) { Debug.LogError($"[WindowsBleNativePlugin] Callback error: {ex}"); }
                }, null);
            }
            else
            {
                // Fallback: no captured context (should not occur once
                // Initialize() has run on the Unity main thread).
                try { action(); }
                catch (Exception ex) { Debug.LogError($"[WindowsBleNativePlugin] Callback error: {ex}"); }
            }
        }

        // Static callback methods (must be static for AOT). Each marshals its
        // body to the Unity main thread via Dispatch.
        [MonoPInvokeCallback(typeof(OnPeripheralFoundDelegate))]
        private static void OnDeviceDiscoveredCallback(string deviceJson)
        {
            Dispatch(() =>
            {
                Debug.Log($"[WindowsBleNativePlugin] Device discovered: {deviceJson}");
                var dto = JsonUtility.FromJson<PeripheralDTO>(deviceJson);
                var device = new WindowsBlePeripheral(dto);
                _discoveredPeripherals.Add(device);
                BleScanEventDelegates.InvokeDeviceDiscovered(device);
            });
        }

        [MonoPInvokeCallback(typeof(OnConnectedDelegate))]
        private static void OnDeviceConnectedCallback(string deviceJson)
        {
            Dispatch(() =>
            {
                var dto = PeripheralDTO.FromJson(deviceJson);
                foreach (var p in _discoveredPeripherals)
                {
                    if (p.UUID == dto.uuid)
                    {
                        BleDeviceEvents.InvokeConnected(p);
                        Debug.Log($"[WindowsBleNativePlugin] Device connected: {p.UUID}");
                        return;
                    }
                }

                var connectedDevice = new WindowsBlePeripheral(dto);
                _discoveredPeripherals.Add(connectedDevice);
                BleDeviceEvents.InvokeConnected(connectedDevice);
                Debug.Log($"[WindowsBleNativePlugin] Device connected: {connectedDevice.UUID}");
            });
        }

        [MonoPInvokeCallback(typeof(OnDisconnectedDelegate))]
        private static void OnDeviceDisconnectedCallback(string deviceUuid)
        {
            Dispatch(() =>
            {
                Debug.Log($"[WindowsBleNativePlugin] Device disconnected: {deviceUuid}");
                BleDeviceEvents.InvokeDisconnected(deviceUuid);
            });
        }

        [MonoPInvokeCallback(typeof(OnBleErrorDelegate))]
        private static void OnErrorCallback(string errorJson)
        {
            Dispatch(() => Debug.LogError($"[WindowsBleNativePlugin] Error: {errorJson}"));
        }

        [MonoPInvokeCallback(typeof(OnLogReceivedDelegate))]
        private static void OnLogCallback(string logMessage)
        {
            Dispatch(() =>
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
            });
        }

        [MonoPInvokeCallback(typeof(OnBleStatusChangedDelegate))]
        private static void OnBleStatusChangedCallback(int status)
        {
            Dispatch(() =>
            {
                Debug.Log($"[WindowsBleNativePlugin] BLE status changed: {status}");
                BleScanEventDelegates.InvokeBLEStatusChanged((BleStatus)status);
            });
        }

        [MonoPInvokeCallback(typeof(OnClearFoundDevicesDelegate))]
        private static void OnClearFoundDevices()
        {
            Dispatch(() =>
            {
                Debug.Log("[WindowsBleNativePlugin] Found devices cleared");
                foreach (var device in _discoveredPeripherals)
                {
                    device.Dispose();
                }
                _discoveredPeripherals.Clear();
                BleScanEventDelegates.InvokeFoundDevicesCleared();
            });
        }

        [MonoPInvokeCallback(typeof(OnServiceFoundDelegate))]
        private static void OnServiceDiscovered(string serviceJson)
        {
            Dispatch(() =>
            {
                Debug.Log($"[WindowsBleNativePlugin] Service found: {serviceJson}");
                ServiceDTO serviceDTO = JsonUtility.FromJson<ServiceDTO>(serviceJson);
                var dto = WindowsBleService.FromDTO(serviceDTO);
                BleDeviceEvents.InvokeServicesDiscovered(dto);
            });
        }

        [MonoPInvokeCallback(typeof(OnCharacteristicFoundDelegate))]
        private static void OnCharacteristicDiscovered(string characteristicJson)
        {
            Dispatch(() =>
            {
                Debug.Log($"[WindowsBleNativePlugin] Characteristic found: {characteristicJson}");
                CharacteristicDTO characteristicDTO = JsonUtility.FromJson<CharacteristicDTO>(characteristicJson);
                BleDeviceEvents.InvokeCharacteristicDiscovered(WindowsBleCharacteristic.FromDTO(characteristicDTO));
            });
        }

        [MonoPInvokeCallback(typeof(OnWriteCharacteristicCompletedDelegate))]
        private static void OnWriteCharacteristicCompleted(string characteristicUUID, int result, string errorDescription)
        {
            Dispatch(() =>
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
            });
        }

        [MonoPInvokeCallback(typeof(OnReadRSSICompletedDelegate))]
        private static void OnReadRSSICompleted(int result, string errorDescription)
        {
            Dispatch(() =>
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
            });
        }

        [MonoPInvokeCallback(typeof(OnDataReceivedDelegate))]
        private static void OnDataReceived(string characteristicUuid, string base64Data)
        {
            Dispatch(() =>
            {
                Debug.Log($"[WindowsBleNativePlugin] Data received from {characteristicUuid}: {base64Data}");
                var bytes = Convert.FromBase64String(base64Data);
                var encodedStr = Encoding.ASCII.GetString(bytes);
                BleDeviceEvents.InvokeDataReceived(characteristicUuid, encodedStr);
            });
        }

        public static bool IsInitialized { get; private set; } = false;

        private static readonly object _lock = new object();

        /// <summary>
        /// Initialize the Windows BLE plugin and register callbacks.
        /// </summary>
        public static void Initialize()
        {
            lock (_lock)
            {
                if (IsInitialized)
                {
                    Debug.Log("[WindowsBleNativePlugin] Already initialized");
                    return;
                }
            }

            // Capture the Unity main-thread SynchronizationContext so native
            // callbacks (raised on WinRT thread-pool threads) can be marshaled
            // back to the main thread. Initialize() is invoked from
            // InitializeAsync() on the main thread.
            _mainContext = SynchronizationContext.Current;

            // Register callbacks first (using the rooted delegate instances).
            UnityBLE_registerOnPeripheralDiscovered(_cbPeripheralFound);
            UnityBLE_registerOnPeripheralConnected(_cbConnected);
            UnityBLE_registerOnPeripheralDisconnected(_cbDisconnected);
            UnityBLE_registerOnBleErrorDetected(_cbError);
            UnityBLE_registerOnBleStateChanged(_cbStateChanged);
            UnityBLE_registerOnDiscoveredPeripheralCleared(_cbCleared);
            UnityBLE_registerOnDiscoveredServices(_cbService);
            UnityBLE_registerOnDiscoveredCharacteristics(_cbCharacteristic);
            UnityBLE_registerOnLog(_cbLog);
            UnityBLE_registerOnWriteCharacteristicCompleted(_cbWriteCompleted);
            UnityBLE_registerOnReadRSSICompleted(_cbReadRssi);
            UnityBLE_registerOnValueReceived(_cbValueReceived);

            Debug.Log("[WindowsBleNativePlugin] Initialize completed.");

            lock (_lock)
            {
                IsInitialized = true;
            }
        }

        /// <summary>
        /// Start BLE scanning with an optional filter.
        /// </summary>
        public static void StartScan(ScanFilter filter = null)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("Plugin not initialized. Call Initialize() first.");
            }

            string serviceUuidsCsv = string.Empty;
            string nameFilter = null;

            if (filter != null)
            {
                if (filter.ServiceUuids != null && filter.ServiceUuids.Length > 0)
                {
                    serviceUuidsCsv = string.Join(",", filter.ServiceUuids);
                }
                if (!string.IsNullOrEmpty(filter.Name))
                {
                    nameFilter = filter.Name;
                }
            }

            int scanResult = UnityBLE_StartScanning(serviceUuidsCsv, nameFilter);

            if (scanResult == 1)
            {
                Debug.Log("[WindowsBleNativePlugin] Scanning already started.");
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

            return UnityBLE_IsScanning() != 0;
        }

        public static BleStatus GetBleStatus()
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("Plugin not initialized. Call Initialize() first.");
            }

            var raw = UnityBLE_GetState();
            Debug.Log($"[WindowsBleNativePlugin] Current BLE state: {raw}");
            return (BleStatus)raw;
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
                Debug.LogError($"[WindowsBleNativePlugin] Failed to connect to device {peripheralUUID}. Error code: {result}");
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
                Debug.LogError($"[WindowsBleNativePlugin] Failed to disconnect from device {address}. Error code: {result}");
            }
            else
            {
                Debug.Log($"[WindowsBleNativePlugin] Successfully disconnected from device {address}");
            }
        }

        public static int StartDiscoveryService(IBlePeripheral peripheral)
        {
            Debug.Log($"[WindowsBleNativePlugin] Starting service discovery for peripheral {peripheral.UUID}");
            if (!IsInitialized)
            {
                throw new InvalidOperationException("Plugin not initialized. Call Initialize() first.");
            }

            if (!peripheral.IsConnected)
            {
                Debug.LogError($"[WindowsBleNativePlugin] Cannot discover services for disconnected device {peripheral.UUID}");
                return -1;
            }

            return UnityBLE_DiscoverServices(peripheral.UUID);
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
                Debug.Log($"Successfully requested read of characteristic {characteristicUUID} from service {serviceUUID} on device {address}");
            }
        }

        /// <summary>
        /// Write a characteristic value.
        /// </summary>
        public static void WriteCharacteristic(string address, string serviceUUID, string characteristicUUID, byte[] data, bool withResponse = true)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("Plugin not initialized. Call Initialize() first.");
            }

            var result = UnityBLE_WriteCharacteristic(address, serviceUUID, characteristicUUID, data, data?.Length ?? 0, withResponse ? 1 : 0);
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

            var result = UnityBLE_UnsubscribeFromCharacteristic(peripheralUUID, serviceUUID, characteristicUUID);
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
