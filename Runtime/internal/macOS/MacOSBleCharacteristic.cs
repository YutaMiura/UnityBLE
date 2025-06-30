using System;
using System.Threading;
using System.Threading.Tasks;
using UnityBLE.macOS;
using UnityEngine;

namespace UnityBLE
{
    /// <summary>
    /// macOS implementation of IBleCharacteristic using Core Bluetooth via native plugin.
    /// </summary>
    public class MacOSBleCharacteristic : IBleCharacteristic, IDisposable
    {
        private readonly string _name;
        private readonly string _uuid;
        private readonly MacOSBleService _service;
        private readonly CharacteristicProperties _properties;
        private byte[] _value;

        private MacOSSubscribeCharacteristicCommand _subscribeCommand;
        private bool _disposed;

        public string Name => _name;
        public string Uuid => _uuid;
        public byte[] Value => _value;
        public CharacteristicProperties Properties => _properties;

        public event Action<byte[]> OnDataReceived;
        public MacOSBleCharacteristic(string name, string uuid, MacOSBleService service, CharacteristicProperties properties)
        {
            _name = name ?? throw new ArgumentNullException(nameof(name));
            _uuid = uuid ?? throw new ArgumentNullException(nameof(uuid));
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _properties = properties;
            _value = new byte[0];
        }

        public async Task<byte[]> ReadAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(MacOSBleCharacteristic));

            if (!Properties.CanRead())
            {
                throw new InvalidOperationException($"Characteristic {_name} ({_uuid}) does not support reading");
            }

            var readCommand = new MacOSReadCharacteristicCommand(_service.DeviceAddress, _service.Uuid, _uuid);
            try
            {
                var result = await readCommand.ExecuteAsync(cancellationToken);
                _value = result;
                return result;
            }
            finally
            {
                readCommand.Dispose();
            }
        }

        public async Task WriteAsync(byte[] data, CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(MacOSBleCharacteristic));

            if (!Properties.CanWrite())
            {
                throw new InvalidOperationException($"Characteristic {_name} ({_uuid}) does not support writing");
            }

            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            // Use write with response by default for reliability
            var writeCommand = new MacOSWriteCharacteristicCommand(_service.DeviceAddress, _service.Uuid, _uuid, data, true);
            try
            {
                await writeCommand.ExecuteAsync(cancellationToken);

                // Update local value cache
                _value = new byte[data.Length];
                Array.Copy(data, _value, data.Length);
            }
            finally
            {
                writeCommand.Dispose();
            }
        }

        public async Task SubscribeAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(MacOSBleCharacteristic));

            if (!Properties.CanNotify())
            {
                throw new InvalidOperationException($"Characteristic {_name} ({_uuid}) does not support notifications");
            }

            if (_subscribeCommand != null && _subscribeCommand.IsSubscribed)
            {
                Debug.LogWarning($"[macOS BLE] Already subscribed to characteristic {_name}");
                return;
            }

            // Clean up any existing subscription command
            _subscribeCommand?.Dispose();

            // Create new subscription command
            _subscribeCommand = new MacOSSubscribeCharacteristicCommand(_service.DeviceAddress, _service.Uuid, _uuid);

            await _subscribeCommand.ExecuteAsync(OnDataReceived, cancellationToken);
        }

        public async Task UnsubscribeAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(MacOSBleCharacteristic));

            if (!Properties.CanNotify())
            {
                throw new InvalidOperationException($"Characteristic {_name} ({_uuid}) does not support notifications");
            }

            if (_subscribeCommand == null || !_subscribeCommand.IsSubscribed)
            {
                Debug.LogWarning($"[macOS BLE] Not subscribed to characteristic {_name}");
                return;
            }

            var unsubscribeCommand = new MacOSUnsubscribeCharacteristicCommand(_service.DeviceAddress, _service.Uuid, _uuid);
            try
            {
                await unsubscribeCommand.ExecuteAsync(cancellationToken);

                // Clean up subscription command
                _subscribeCommand?.Dispose();
                _subscribeCommand = null;
            }
            finally
            {
                unsubscribeCommand.Dispose();
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _subscribeCommand?.Dispose();
                _subscribeCommand = null;
                _disposed = true;
            }
        }

        public override string ToString()
        {
            return $"MacOSBleCharacteristic: {_name} ({_uuid}) Properties: {_properties}";
        }
    }
}