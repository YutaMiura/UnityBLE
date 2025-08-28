using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityBLE.Android
{
    public class AndroidBleCharacteristic : IBleCharacteristic
    {
        public string Uuid { get; private set; }
        public string serviceUUID { get; private set; }
        public string peripheralUUID { get; private set; }
        public CharacteristicProperties Properties { get; private set; }

        public event IBleCharacteristic.DataReceivedDelegate OnDataReceived;

        private readonly SubscribeCommand _subscribeCommand;

        private AndroidBleCharacteristic(string uuid, string serviceUUID, string peripheralUUID, CharacteristicProperties properties)
        {
            Uuid = uuid;
            this.serviceUUID = serviceUUID;
            this.peripheralUUID = peripheralUUID;
            Properties = properties;
            _subscribeCommand = NativeFacade.Instance.CreateSubscribeCommand(this, OnCharacteristicValueReceived);
        }

        private void OnCharacteristicValueReceived(string data)
        {
            OnDataReceived?.Invoke(data);
        }

        internal static AndroidBleCharacteristic FromDTO(CharacteristicDTO dto)
        {
            if (dto == null)
            {
                throw new System.ArgumentNullException(nameof(dto));
            }

            var properties = CharacteristicProperties.None;
            if (dto.isReadable)
            {
                properties |= CharacteristicProperties.Read;
            }
            if (dto.isWritable)
            {
                properties |= CharacteristicProperties.Write;
            }
            if (dto.isNotifiable)
            {
                properties |= CharacteristicProperties.Notify;
            }

            return new AndroidBleCharacteristic
            (
                dto.uuid,
                dto.serviceUUID,
                dto.peripheralUUID,
                properties
            );
        }

        public Task<string> ReadAsync(CancellationToken cancellationToken = default)
        {
            return NativeFacade.Instance.ReadAsync(this);
        }

        public void Subscribe()
        {
            if (!Properties.CanNotify())
            {
                throw new InvalidOperationException($"Characteristic ({Uuid}) does not support notifications");
            }

            if (_subscribeCommand.IsSubscribed)
            {
                Debug.LogWarning($" Already subscribed to characteristic ({Uuid})");
                return;
            }

            _subscribeCommand.Execute();
        }

        public async Task UnsubscribeAsync()
        {
            if (!Properties.CanNotify())
            {
                throw new InvalidOperationException($"Characteristic ({Uuid}) does not support notifications");
            }

            if (_subscribeCommand == null || !_subscribeCommand.IsSubscribed)
            {
                Debug.LogWarning($" Not subscribed to characteristic ({Uuid})");
                return;
            }

            await _subscribeCommand?.UnsubscribeAsync();
        }

        public Task WriteAsync(byte[] data, CancellationToken cancellationToken = default)
        {
            return NativeFacade.Instance.WriteAsync(this, data);
        }

        public override string ToString()
        {
            return $"AndroidBleCharacteristic: ({Uuid} serviceUUID:{serviceUUID} peripheralUUID:{peripheralUUID}) Properties: {Properties}";
        }

        public void Dispose()
        {
            ;
        }
    }
}