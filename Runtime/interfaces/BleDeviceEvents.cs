using System;

namespace UnityBLE
{
    internal static class BleDeviceEvents
    {
        public static event Action<IBlePeripheral> OnConnected;
        public delegate void DisconnectedDelegate(string deviceUuid);
        public static event DisconnectedDelegate OnDisconnected;
        public static event Action<IBleService> OnServicesDiscovered;
        public delegate void DataReceivedDelegate(string characteristicUuid, string data);
        public static event DataReceivedDelegate OnDataReceived;

        public delegate void ReadRSSICompletedDelegate(int rssi);

        public static event ReadRSSICompletedDelegate OnReadRSSICompleted;

        public static event Action<IBleCharacteristic> OnCharacteristicDiscovered;

        public delegate void WriteOperationCompletedDelegate(string characteristicUuid, bool success);
        public static event WriteOperationCompletedDelegate OnWriteOperationCompleted;

        internal static void InvokeConnected(IBlePeripheral device)
        {
            OnConnected?.Invoke(device);
        }

        internal static void InvokeDisconnected(string deviceUuid)
        {
            OnDisconnected?.Invoke(deviceUuid);
        }

        internal static void InvokeServicesDiscovered(IBleService service)
        {
            OnServicesDiscovered?.Invoke(service);
        }

        internal static void InvokeDataReceived(string characteristicUuid, string data)
        {
            OnDataReceived?.Invoke(characteristicUuid, data);
        }

        internal static void InvokeReadRSSICompleted(int rssi)
        {
            OnReadRSSICompleted?.Invoke(rssi);
        }

        internal static void InvokeWriteOperationCompleted(string characteristicUuid, bool success)
        {
            OnWriteOperationCompleted?.Invoke(characteristicUuid, success);
        }

        internal static void InvokeCharacteristicDiscovered(IBleCharacteristic characteristic)
        {
            OnCharacteristicDiscovered?.Invoke(characteristic);
        }
    }
}