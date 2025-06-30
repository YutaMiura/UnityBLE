using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityBLE;

namespace UnityBLE.iOS
{
    /// <summary>
    /// iOS implementation of IBleCharacteristic for real iOS devices.
    /// </summary>
    public class iOSBleCharacteristic : IBleCharacteristic
    {
        private readonly string _name;
        private readonly string _uuid;
        private readonly string _deviceAddress;
        private readonly string _serviceUuid;
        private byte[] _value;
        private readonly CharacteristicProperties _properties;

        private bool _isSubscribed;

        // Command instances for reuse
        private iOSReadCharacteristicCommand _readCommand;
        private iOSWriteCharacteristicCommand _writeCommand;
        private iOSSubscribeCharacteristicCommand _subscribeCommand;
        private iOSUnsubscribeCharacteristicCommand _unsubscribeCommand;

        public string Name => _name;
        public string Uuid => _uuid;
        public byte[] Value => _value;
        public CharacteristicProperties Properties => _properties;
        public event Action<byte[]> OnDataReceived;

        public iOSBleCharacteristic(string name, string uuid, string deviceAddress, string serviceUuid, CharacteristicProperties properties)
        {
            _name = name;
            _uuid = uuid;
            _deviceAddress = deviceAddress;
            _serviceUuid = serviceUuid;
            _value = new byte[0];
            _properties = properties;

            // Initialize commands
            _readCommand = new iOSReadCharacteristicCommand(_deviceAddress, _serviceUuid, _uuid);
            _writeCommand = new iOSWriteCharacteristicCommand(_deviceAddress, _serviceUuid, _uuid);
            _subscribeCommand = new iOSSubscribeCharacteristicCommand(_deviceAddress, _serviceUuid, _uuid);
            _unsubscribeCommand = new iOSUnsubscribeCharacteristicCommand(_deviceAddress, _serviceUuid, _uuid);
        }

        public async Task<byte[]> ReadAsync(CancellationToken cancellationToken = default)
        {
            if (!Properties.CanRead())
            {
                throw new InvalidOperationException($"Characteristic {_name} ({_uuid}) does not support reading");
            }

            Debug.Log($"[iOS BLE] Reading characteristic {_name} ({_uuid})...");

            try
            {
                _value = await _readCommand.ExecuteAsync(cancellationToken);
                Debug.Log($"[iOS BLE] Read {_value.Length} bytes from characteristic {_name}");
                return _value;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[iOS BLE] Error reading characteristic {_name}: {ex.Message}");
                throw;
            }
        }

        public async Task WriteAsync(byte[] data, CancellationToken cancellationToken = default)
        {
            if (!Properties.CanWrite())
            {
                throw new InvalidOperationException($"Characteristic {_name} ({_uuid}) does not support writing");
            }

            Debug.Log($"[iOS BLE] Writing {data.Length} bytes to characteristic {_name} ({_uuid})...");

            try
            {
                await _writeCommand.ExecuteAsync(data, cancellationToken);

                _value = new byte[data.Length];
                Array.Copy(data, _value, data.Length);

                Debug.Log($"[iOS BLE] Successfully wrote {data.Length} bytes to characteristic {_name}");

                // If subscribed, simulate notification
                if (_isSubscribed && Properties.CanNotify())
                {
                    OnDataReceived?.Invoke(_value);
                    Debug.Log($"[iOS BLE] Sent notification for characteristic {_name}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[iOS BLE] Error writing characteristic {_name}: {ex.Message}");
                throw;
            }
        }

        public Task StartNotificationsAsync(Action<byte[]> onNotification, CancellationToken cancellationToken = default)
        {
            if (!Properties.CanNotify())
            {
                throw new InvalidOperationException($"Characteristic {_name} ({_uuid}) does not support notifications");
            }

            Debug.Log($"[iOS BLE] Starting notifications for characteristic {_name} ({_uuid})");

            // TODO: Implement native notification subscription through events
            return Task.CompletedTask;
        }

        public Task StopNotificationsAsync(CancellationToken cancellationToken = default)
        {
            if (!Properties.CanNotify())
            {
                throw new InvalidOperationException($"Characteristic {_name} ({_uuid}) does not support notifications");
            }

            Debug.Log($"[iOS BLE] Stopping notifications for characteristic {_name} ({_uuid})");

            // TODO: Implement native notification unsubscription
            return Task.CompletedTask;
        }

        public override string ToString()
        {
            return $"iOSBleCharacteristic: {_name} ({_uuid}) Properties: {_properties}";
        }


        public async Task SubscribeAsync(CancellationToken cancellationToken = default)
        {
            if (!Properties.CanNotify())
            {
                throw new InvalidOperationException($"Characteristic {_name} ({_uuid}) does not support notifications");
            }

            if (_isSubscribed)
            {
                Debug.LogWarning($"[iOS BLE] Already subscribed to characteristic {_name}");
                return;
            }

            Debug.Log($"[iOS BLE] Subscribing to notifications for characteristic {_name} ({_uuid})");

            try
            {
                await _subscribeCommand.ExecuteAsync(OnDataReceived, cancellationToken);
                _isSubscribed = true;

                Debug.Log($"[iOS BLE] Successfully subscribed to characteristic {_name}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[iOS BLE] Error subscribing to characteristic {_name}: {ex.Message}");
                throw;
            }
        }

        public async Task UnsubscribeAsync(CancellationToken cancellationToken = default)
        {
            if (!Properties.CanNotify())
            {
                throw new InvalidOperationException($"Characteristic {_name} ({_uuid}) does not support notifications");
            }

            if (!_isSubscribed)
            {
                Debug.LogWarning($"[iOS BLE] Not subscribed to characteristic {_name}");
                return;
            }

            Debug.Log($"[iOS BLE] Unsubscribing from notifications for characteristic {_name} ({_uuid})");

            try
            {
                await _unsubscribeCommand.ExecuteAsync(cancellationToken);
                _isSubscribed = false;

                Debug.Log($"[iOS BLE] Successfully unsubscribed from characteristic {_name}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[iOS BLE] Error unsubscribing from characteristic {_name}: {ex.Message}");
                throw;
            }
        }
    }
}