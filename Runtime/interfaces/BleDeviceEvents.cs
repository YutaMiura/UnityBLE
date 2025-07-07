using System;

namespace UnityBLE
{
    /// <summary>
    /// Represents notification data from a characteristic.
    /// </summary>
    public class BleCharacteristicNotification
    {
        public string ServiceUuid { get; }
        public string CharacteristicUuid { get; }
        public byte[] Data { get; }

        public BleCharacteristicNotification(string serviceUuid, string characteristicUuid, byte[] data)
        {
            ServiceUuid = serviceUuid;
            CharacteristicUuid = characteristicUuid;
            Data = data;
        }
    }

    public class BleDeviceEvents
    {
        public event Action<IBleDevice> OnConnected;
        public event Action<IBleDevice> OnDisconnected;
        public event Action<IBleDevice> OnServicesChanged;
        public event Action<string, byte[]> OnDataReceived;

        internal void RaiseConnected(IBleDevice device)
        {
            OnConnected?.Invoke(device);
        }

        internal void RaiseDisconnected(IBleDevice device)
        {
            OnDisconnected?.Invoke(device);
        }

        internal void RaiseServicesChanged(IBleDevice device)
        {
            OnServicesChanged?.Invoke(device);
        }

        internal void RaiseDataReceived(string characteristicUuid, byte[] data)
        {
            OnDataReceived?.Invoke(characteristicUuid, data);
        }
    }
}