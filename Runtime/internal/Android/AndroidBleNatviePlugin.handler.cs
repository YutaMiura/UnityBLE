using System;
using System.Collections.Concurrent;
using UnityEngine;

namespace UnityBLE.Android
{
    internal partial class AndroidBleNativePlugin
    {
        private NativeLogHandler logReceiver;
        private NativeEventHandler eventReceiver;

        internal event Action<int> OnScanResult
        {
            add => eventReceiver.listener.OnScanResult += value;
            remove => eventReceiver.listener.OnScanResult -= value;
        }

        internal event Action<int> OnStopScanResult
        {
            add => eventReceiver.listener.OnStopScanResult += value;
            remove => eventReceiver.listener.OnStopScanResult -= value;
        }

        internal event Action<int> OnConnectResult
        {
            add => eventReceiver.listener.OnConnectResult += value;
            remove => eventReceiver.listener.OnConnectResult -= value;
        }

        internal event Action<int> OnDiscoveryServiceResult
        {
            add => eventReceiver.listener.OnDiscoveryServiceResult += value;
            remove => eventReceiver.listener.OnDiscoveryServiceResult -= value;
        }

        private void CreateHandlers()
        {
            logReceiver = NativeLogHandler.Instance;
            eventReceiver = NativeEventHandler.Instance;
        }


        private class NativeLogHandler : MonoBehaviour, IDisposable
        {
            private const string GAMEOBJ_NAME = "UnityBLELogHandler";

            private static NativeLogHandler _instance;
            public static NativeLogHandler Instance
            {
                get
                {
                    _instance = FindFirstObjectByType<NativeLogHandler>();
                    if (_instance == null)
                    {
                        var obj = new GameObject(GAMEOBJ_NAME);
                        _instance = obj.AddComponent<NativeLogHandler>();
                        DontDestroyOnLoad(obj);
                    }
                    return _instance;
                }
            }

            public void OnReceiveLogDebug(string msg)
            {
                Debug.Log(msg);
            }

            public void OnReceiveLogWarn(string msg)
            {
                Debug.LogWarning(msg);
            }

            public void OnReceiveLogError(string msg)
            {
                Debug.LogError(msg);
            }

            public void Dispose()
            {
                Destroy(gameObject);
            }
        }

        private class NativeEventHandler : MonoBehaviour, IDisposable
        {
            private const string GAMEOBJ_NAME = "AndroidBleEventListener";
            private static NativeEventHandler _instance;

            private ConcurrentDictionary<string, AndroidBlePeripheral> _discoveredDevices = new();

            internal class Listener
            {
                public event Action<int> OnScanResult;
                public event Action<int> OnStopScanResult;
                public event Action<int> OnConnectResult;
                public event Action<int> OnDiscoveryServiceResult;

                public delegate void ReadOperationResultDelegate(string from, int status, byte[] data);

                public event ReadOperationResultDelegate OnReadResult;

                public delegate void WriteOperationResultDelegate(string from, int status);

                public event WriteOperationResultDelegate OnWriteResult;

                public delegate void UnsubscribeResultDelegate(string from, int status);
                public event UnsubscribeResultDelegate OnUnsubscribeResult;

                internal void InvokeScanResult(int code)
                {
                    OnScanResult?.Invoke(code);
                }

                internal void InvokeStopScanResult(int code)
                {
                    OnStopScanResult?.Invoke(code);
                }

                internal void InvokeConnectResult(int code)
                {
                    OnConnectResult?.Invoke(code);
                }

                internal void InvokeDiscoveryServiceResult(int code)
                {
                    OnDiscoveryServiceResult?.Invoke(code);
                }

                internal void InvokeReadResult(string from, int status, byte[] data)
                {
                    OnReadResult?.Invoke(from, status, data);
                }

                internal void InvokeWriteResult(string from, int status)
                {
                    OnWriteResult?.Invoke(from, status);
                }

                internal void InvokeUnsubscribed(string from, int status)
                {
                    OnUnsubscribeResult?.Invoke(from, status);
                }
            }

            internal Listener listener { get; } = new Listener();

            public static NativeEventHandler Instance
            {
                get
                {
                    _instance = FindFirstObjectByType<NativeEventHandler>();
                    if (_instance == null)
                    {
                        var obj = new GameObject(GAMEOBJ_NAME);
                        _instance = obj.AddComponent<NativeEventHandler>();
                        DontDestroyOnLoad(obj);
                    }
                    return _instance;
                }
            }

            public void OnDeviceDiscovered(string deviceJson)
            {
                var dto = JsonUtility.FromJson<PeripheralDTO>(deviceJson);
                var device = new AndroidBlePeripheral(dto);
                if (_discoveredDevices.TryAdd(device.UUID, device))
                {
                    BleScanEventDelegates.InvokeDeviceDiscovered(device);
                }
                else
                {
                    Debug.LogWarning($"Device {device.UUID} already discovered.");
                }
            }

            public void OnConnected(string deviceJson)
            {
                var dto = JsonUtility.FromJson<PeripheralDTO>(deviceJson);
                Debug.Log($"Device {dto.uuid} connected.");
                if (_discoveredDevices.TryGetValue(dto.uuid, out var device))
                {
                    BleDeviceEvents.InvokeConnected(device);
                }
                else
                {
                    Debug.LogWarning($"Device {dto.uuid} not found in discovered devices.");
                }
            }

            public void OnServiceDiscovered(string serviceJson)
            {
                ServiceDTO dto = JsonUtility.FromJson<ServiceDTO>(serviceJson);
                var service = AndroidBleService.FromDTO(dto);
                BleDeviceEvents.InvokeServicesDiscovered(service);

                foreach (var characteristic in service.Characteristics)
                {
                    BleDeviceEvents.InvokeCharacteristicDiscovered(characteristic);
                }
            }

            public void OnReadCharacteristic(string readResponseJson)
            {
                var dto = JsonUtility.FromJson<ReadResponseDTO>(readResponseJson);
                if (dto.status == 0)
                {
                    var data = Convert.FromBase64String(dto.value);
                    listener.InvokeReadResult(dto.from, dto.status, data);
                }
                else
                {
                    listener.InvokeReadResult(dto.from, dto.status, null);
                }
            }

            public void OnWriteCharacteristic(string writeResponseJson)
            {
                var dto = JsonUtility.FromJson<WriteResponseDTO>(writeResponseJson);
                listener.InvokeWriteResult(dto.from, dto.status);
            }

            public void OnSubscribed(string subscribeResponseJson)
            {
                var dto = JsonUtility.FromJson<SubscribeResponseDTO>(subscribeResponseJson);
                if (dto.status == 0)
                {
                    BleDeviceEvents.InvokeDataReceived(dto.from, dto.value);
                }
                else
                {
                    Debug.LogError($"Failed to subscribe to characteristic {dto.from}: status {dto.status}");
                }
            }

            public void OnUnsubscribed(string unsubscribeResponseJson)
            {
                var dto = JsonUtility.FromJson<SubscribeResponseDTO>(unsubscribeResponseJson);
                if (dto.status == 0)
                {
                    listener.InvokeUnsubscribed(dto.from, dto.status);
                }
                else
                {
                    Debug.LogError($"Failed to unsubscribe from characteristic {dto.from}: status {dto.status}");
                }
            }

            public void OnClearFoundDevices(string _)
            {
                _discoveredDevices.Clear();
                BleScanEventDelegates.InvokeFoundDevicesCleared();
            }

            public void OnScanResult(string code)
            {
                listener.InvokeScanResult(int.Parse(code));
            }

            public void OnStopScanResult(string code)
            {
                listener.InvokeStopScanResult(int.Parse(code));
            }

            public void OnConnectResult(string code)
            {
                listener.InvokeConnectResult(int.Parse(code));
            }

            public void OnDiscoveryServiceResult(string code)
            {
                listener.InvokeDiscoveryServiceResult(int.Parse(code));
            }

            public void Dispose()
            {
                Destroy(gameObject);
            }
        }
    }
}