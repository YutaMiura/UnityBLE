using System;
using UnityEngine;

namespace UnityBLE
{
    /// <summary>
    /// Singleton dispatcher for BLE native plugin events.
    /// </summary>
    internal class BleNativePluginEventDispatcher : MonoBehaviour
    {
        private static BleNativePluginEventDispatcher instance;

        public static BleNativePluginEventDispatcher Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject("BleNativePluginEventDispatcher");
                    instance = go.AddComponent<BleNativePluginEventDispatcher>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        public event Action<string> DeviceDiscovered;
        public event Action<string> DeviceConnected;
        public event Action<string> DeviceDisconnected;
        public event Action<string> ConnectionFailed;
        public event Action<string> ServicesDiscovered;

        public event Action ScanCompleted;

        public event Action<string> StopScanFailed;

        // Native plugin callback methods (called from native code via SendMessage)
        // These have Unity-specific naming conventions for native plugin integration
        public void OnDeviceDiscovered(string deviceJson)
        {
            DeviceDiscovered?.Invoke(deviceJson);
        }

        public void OnDeviceConnected(string deviceAddress)
        {
            DeviceConnected?.Invoke(deviceAddress);
        }

        public void OnDeviceDisconnected(string deviceAddress)
        {
            DeviceDisconnected?.Invoke(deviceAddress);
        }

        public void OnConnectionFailed(string error)
        {
            ConnectionFailed?.Invoke(error);
        }

        public void OnServicesDiscovered(string servicesJson)
        {
            ServicesDiscovered?.Invoke(servicesJson);
        }

        public void OnStopScanFailed(string err)
        {
            StopScanFailed?.Invoke(err);
        }

        public void OnScanCompleted()
        {
            ScanCompleted?.Invoke();
        }
    }
}