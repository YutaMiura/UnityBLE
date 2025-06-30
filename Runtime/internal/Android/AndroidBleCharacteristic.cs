using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityBLE.Android;

namespace UnityBLE
{
    /// <summary>
    /// Android implementation of IBleCharacteristic using Command pattern for BLE operations.
    /// </summary>
    public class AndroidBleCharacteristic : IBleCharacteristic
    {
        private readonly string _uuid;
        private readonly IBleService _service;
        private readonly CharacteristicProperties _properties;
        private AndroidSubscribeCharacteristicCommand _subscribeCommand;
        private bool _isSubscribed;

        public string Uuid => _uuid;
        public IBleService Service => _service;
        public CharacteristicProperties Properties => _properties;

        public event Action<byte[]> OnDataReceived;

        public AndroidBleCharacteristic(string uuid, IBleService service, CharacteristicProperties properties)
        {
            _uuid = uuid;
            _service = service;
            _properties = properties;
        }

        public async Task<byte[]> ReadAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!Properties.CanRead())
            {
                throw new InvalidOperationException($"Characteristic {_uuid} does not support reading");
            }

            var readCommand = new AndroidReadCharacteristicCommand(_service.DeviceAddress, _service.Uuid, _uuid);
            try
            {
                return await readCommand.ExecuteAsync(cancellationToken);
            }
            finally
            {
                readCommand.Dispose();
            }
        }

        public async Task WriteAsync(byte[] data, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!Properties.CanWrite())
            {
                throw new InvalidOperationException($"Characteristic {_uuid} does not support writing");
            }

            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            // Use write with response by default for reliability
            var writeCommand = new AndroidWriteCharacteristicCommand(_service.DeviceAddress, _service.Uuid, _uuid, data, true);
            try
            {
                await writeCommand.ExecuteAsync(cancellationToken);
            }
            finally
            {
                writeCommand.Dispose();
            }
        }

        public async Task SubscribeAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!Properties.CanNotify())
            {
                throw new InvalidOperationException($"Characteristic {_uuid} does not support notifications");
            }

            if (_isSubscribed)
            {
                Debug.LogWarning($"[Android BLE] Already subscribed to characteristic {_uuid}");
                return;
            }

            _subscribeCommand = new AndroidSubscribeCharacteristicCommand(_service.DeviceAddress, _service.Uuid, _uuid);
            try
            {
                await _subscribeCommand.ExecuteAsync(OnDataReceived, cancellationToken);
                _isSubscribed = true;
            }
            catch
            {
                _subscribeCommand?.Dispose();
                _subscribeCommand = null;
                throw;
            }
        }

        public async Task UnsubscribeAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!Properties.CanNotify())
            {
                throw new InvalidOperationException($"Characteristic {_uuid} does not support notifications");
            }

            if (!_isSubscribed)
            {
                Debug.LogWarning($"[Android BLE] Not subscribed to characteristic {_uuid}");
                return;
            }

            var unsubscribeCommand = new AndroidUnsubscribeCharacteristicCommand(_service.DeviceAddress, _service.Uuid, _uuid);
            try
            {
                await unsubscribeCommand.ExecuteAsync(cancellationToken);
                _isSubscribed = false;

                _subscribeCommand?.Dispose();
                _subscribeCommand = null;
            }
            finally
            {
                unsubscribeCommand.Dispose();
            }
        }

    }
}