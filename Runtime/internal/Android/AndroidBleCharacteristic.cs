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

        private AndroidBleCharacteristic()
        {
            _subscribeCommand = NativeFacade.Instance.CreateSubscribeCommand(this, OnDataReceived);
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
            {
                Uuid = dto.uuid,
                serviceUUID = dto.serviceUUID,
                peripheralUUID = dto.peripheralUUID,
                Properties = properties
            };
        }

        public Task<byte[]> ReadAsync(CancellationToken cancellationToken = default)
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

        public Task UnsubscribeAsync()
        {
            if (!Properties.CanNotify())
            {
                throw new InvalidOperationException($"Characteristic ({Uuid}) does not support notifications");
            }

            if (_subscribeCommand == null || !_subscribeCommand.IsSubscribed)
            {
                Debug.LogWarning($" Not subscribed to characteristic ({Uuid})");
                return Task.CompletedTask;
            }

            return NativeFacade.Instance.UnsubscribeAsync(this);
        }

        public Task WriteAsync(byte[] data, CancellationToken cancellationToken = default)
        {
            return NativeFacade.Instance.WriteAsync(this, data);
        }

        public void Dispose()
        {
            ;
        }
    }
}