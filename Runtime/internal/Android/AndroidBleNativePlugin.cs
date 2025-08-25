
#if UNITY_ANDROID

using System;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityBLE.Android
{
    internal partial class AndroidBleNativePlugin : IDisposable
    {
        private AndroidJavaClass BleManagerClass;
        private AndroidJavaObject BleManagerInstance;

        internal AndroidBleNativePlugin()
        {
            BleManagerClass = new AndroidJavaClass(CLASS_BLE_MANAGER);
            BleManagerInstance = BleManagerClass.CallStatic<AndroidJavaObject>("getInstance");
            CreateHandlers();
        }

        public Task StartScanAsync(ScanFilter filter)
        {
            TaskCompletionSource<int> task = new();
            eventReceiver.listener.OnScanResult += OnScanResult;
            var serviceUUIDs = filter.ServiceUuids;
            var names = new String[] {
                filter.Name
            };
            BleManagerInstance.Call(METHOD_NAME_START_SCAN, names, serviceUUIDs);

            return task.Task;

            void OnScanResult(int code)
            {
                if (code == 0)
                {
                    task.TrySetResult(code);
                }
                else if (code == 1)
                {
                    task.TrySetException(new BleUnsupported());
                }
                else if (code == 2)
                {
                    task.TrySetException(new BleUnAuthorized());
                }
                else if (code == 3)
                {
                    task.TrySetException(new BleUnAuthorized());
                }
                else
                {
                    task.TrySetException(new Exception($"Unknown error code: {code}"));
                }
                eventReceiver.listener.OnScanResult -= OnScanResult;
            }
        }

        public Task StopScanAsync()
        {
            TaskCompletionSource<int> task = new();
            eventReceiver.listener.OnStopScanResult += OnStopScanResult;
            BleManagerInstance.Call(METHOD_NAME_STOP_SCAN);

            return task.Task;

            void OnStopScanResult(int code)
            {
                if (code == 0)
                {
                    task.TrySetResult(code);
                }
                else if (code == 1)
                {
                    task.TrySetException(new BleUnsupported());
                }
                else if (code == 2)
                {
                    task.TrySetException(new BleUnAuthorized());
                }
                else if (code == 3)
                {
                    task.TrySetException(new BleUnAuthorized());
                }
                else
                {
                    task.TrySetException(new Exception($"Unknown error code: {code}"));
                }
                eventReceiver.listener.OnStopScanResult -= OnStopScanResult;
            }
        }

        public void Connect(IBlePeripheral device)
        {
            BleManagerInstance.Call(METHOD_NAME_CONNECT, device.UUID);
        }

        public void Disconnect(IBlePeripheral device)
        {
            BleManagerInstance.Call(METHOD_NAME_DISCONNECT, device.UUID);
        }

        public void DiscoveryServices(IBlePeripheral device)
        {
            BleManagerInstance.Call(METHOD_NAME_DISCOVERY_SERVICES, device.UUID);
        }

        public Task<byte[]> ReadAsync(IBleCharacteristic characteristic)
        {
            TaskCompletionSource<byte[]> task = new();
            eventReceiver.listener.OnReadResult += OnReadResult;
            BleManagerInstance.Call(METHOD_NAME_READ, characteristic.Uuid, characteristic.serviceUUID, characteristic.peripheralUUID);

            return task.Task;

            void OnReadResult(string from, int status, byte[] data)
            {
                if (from != characteristic.peripheralUUID)
                {
                    Debug.LogWarning($"Read result from unexpected peripheral: {from}, expected: {characteristic.peripheralUUID}");
                    return;
                }
                if (status == 0)
                {
                    task.TrySetResult(data);
                }
                else
                {
                    task.TrySetException(new Exception($"Unknown error code: {status}"));
                }
                eventReceiver.listener.OnReadResult -= OnReadResult;
            }
        }

        public Task WriteAsync(IBleCharacteristic characteristic, byte[] data)
        {
            TaskCompletionSource<int> task = new();
            eventReceiver.listener.OnWriteResult += OnWriteResult;
            BleManagerInstance.Call(METHOD_NAME_WRITE, characteristic.Uuid, characteristic.serviceUUID, characteristic.peripheralUUID, data);
            return task.Task;

            void OnWriteResult(string from, int status)
            {
                if (from != characteristic.peripheralUUID)
                {
                    Debug.LogWarning($"Write result from unexpected peripheral: {from}, expected: {characteristic.peripheralUUID}");
                    return;
                }
                if (status == 0)
                {
                    task.TrySetResult(status);
                }
                else
                {
                    task.TrySetException(new Exception($"Unknown error code: {status}"));
                }
                eventReceiver.listener.OnWriteResult -= OnWriteResult;
            }

        }
        public void Subscribe(string characteristicUuid, string serviceUuid, string peripheralUuid)
        {
            BleManagerInstance.Call(METHOD_NAME_SUBSCRIBE, characteristicUuid, serviceUuid, peripheralUuid);
        }

        public Task UnsubscribeAsync(string characteristicUuid, string serviceUuid, string peripheralUuid)
        {
            TaskCompletionSource<int> task = new();
            eventReceiver.listener.OnUnsubscribeResult += OnUnsubscribeResult;
            BleManagerInstance.Call(METHOD_NAME_UNSUBSCRIBE, characteristicUuid, serviceUuid, peripheralUuid);

            return task.Task;

            void OnUnsubscribeResult(string from, int status)
            {
                if (from != peripheralUuid)
                {
                    Debug.LogWarning($"Unsubscribe result from unexpected peripheral: {from}, expected: {peripheralUuid}");
                    return;
                }
                if (status == 0)
                {
                    task.TrySetResult(status);
                }
                else
                {
                    task.TrySetException(new Exception($"Unknown error code: {status}"));
                }
                eventReceiver.listener.OnUnsubscribeResult -= OnUnsubscribeResult;
            }
        }

        public void Dispose()
        {
            BleManagerClass?.Dispose();
            BleManagerInstance?.Dispose();
            BleManagerClass = null;
            BleManagerInstance = null;
            if (logReceiver != null)
            {
                logReceiver.Dispose();
                logReceiver = null;
            }
            if (eventReceiver != null)
            {
                eventReceiver.Dispose();
                eventReceiver = null;
            }
        }

    }
}
#endif