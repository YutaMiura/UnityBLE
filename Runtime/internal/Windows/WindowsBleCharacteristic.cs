using System;
using System.Threading;
using System.Threading.Tasks;
using UnityBLE.windows;
using UnityEngine;

namespace UnityBLE
{
    /// <summary>
    /// Windows implementation of <see cref="IBleCharacteristic"/> using the C++/WinRT native plugin.
    /// </summary>
    public class WindowsBleCharacteristic : IBleCharacteristic
    {
        private readonly WindowsSubscribeCharacteristicCommand _subscribeCommand;

        public string Uuid { get; private set; }
        public string serviceUUID { get; private set; }
        public string peripheralUUID { get; private set; }
        public CharacteristicProperties Properties { get; private set; }
        public event IBleCharacteristic.DataReceivedDelegate OnDataReceived;

        public WindowsBleCharacteristic(string uuid, string serviceUuid, string peripheralUuid, CharacteristicProperties properties)
        {
            Uuid = uuid ?? throw new ArgumentNullException(nameof(uuid));
            serviceUUID = serviceUuid ?? throw new ArgumentNullException(nameof(serviceUuid));
            peripheralUUID = peripheralUuid ?? throw new ArgumentNullException(nameof(peripheralUuid));
            Properties = properties;
            _subscribeCommand = new WindowsSubscribeCharacteristicCommand(peripheralUUID, serviceUUID, Uuid, OnDataReceivedHandler);
        }

        private void OnDataReceivedHandler(string data)
        {
            OnDataReceived?.Invoke(data);
        }

        public async Task<string> ReadAsync(CancellationToken cancellationToken = default)
        {
            if (!Properties.CanRead())
            {
                throw new InvalidOperationException($"Characteristic ({Uuid}) does not support reading");
            }

            var readCommand = new WindowsReadCharacteristicCommand(peripheralUUID, serviceUUID, Uuid);
            try
            {
                var result = await readCommand.ExecuteAsync(cancellationToken);
                return result;
            }
            finally
            {
                readCommand.Dispose();
            }
        }

        public async Task WriteAsync(byte[] data, CancellationToken cancellationToken = default)
        {
            if (!Properties.CanWrite())
            {
                throw new InvalidOperationException($"Characteristic ({Uuid}) does not support writing");
            }

            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            // Use write with response by default for reliability
            var writeCommand = new WindowsWriteCharacteristicCommand(peripheralUUID, serviceUUID, Uuid, data, true);
            try
            {
                await writeCommand.ExecuteAsync(cancellationToken);
            }
            finally
            {
                writeCommand.Dispose();
            }
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

            var unsubscribeCommand = new WindowsUnsubscribeCharacteristicCommand(peripheralUUID, serviceUUID, Uuid);
            try
            {
                unsubscribeCommand.Execute();
                return Task.CompletedTask;
            }
            finally
            {
                unsubscribeCommand.Dispose();
            }
        }

        public void Dispose()
        {
            _subscribeCommand?.Dispose();
        }

        public override string ToString()
        {
            return $"WindowsBleCharacteristic: ({Uuid}) Properties: {Properties}";
        }

        internal static WindowsBleCharacteristic FromDTO(CharacteristicDTO dto)
        {
            var properties = CharacteristicProperties.None;
            if (dto.isReadable)
            {
                properties |= CharacteristicProperties.Read;
            }
            if (dto.isWritable)
            {
                properties |= CharacteristicProperties.Write;
            }
            if (dto.isWritableWithoutResponse)
            {
                properties |= CharacteristicProperties.WriteWithoutResponse;
            }
            if (dto.isNotifiable)
            {
                properties |= CharacteristicProperties.Notify;
            }
            return new WindowsBleCharacteristic(dto.uuid, dto.serviceUUID, dto.peripheralUUID, properties);
        }
    }
}
