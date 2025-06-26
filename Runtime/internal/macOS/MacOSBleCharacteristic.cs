using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityBLE
{
    /// <summary>
    /// macOS Unity Editor implementation of IBleCharacteristic for testing purposes.
    /// </summary>
    public class MacOSBleCharacteristic : IBleCharacteristic
    {
        private readonly string _name;
        private readonly string _uuid;
        private byte[] _value;
        private readonly bool _canRead;
        private readonly bool _canWrite;
        private readonly bool _canNotify;

        public string Name => _name;
        public string Uuid => _uuid;
        public byte[] Value => _value;
        public bool CanRead => _canRead;
        public bool CanWrite => _canWrite;
        public bool CanNotify => _canNotify;

        public MacOSBleCharacteristic(string name, string uuid)
        {
            _name = name;
            _uuid = uuid;
            _value = new byte[0];

            _canRead = true;
            _canWrite = uuid.ToLower() != "00002a05-0000-1000-8000-00805f9b34fb"; // Service Changed is read-only
            _canNotify = uuid.ToLower() == "00002a05-0000-1000-8000-00805f9b34fb"; // Service Changed can notify
        }

        public async Task<byte[]> ReadAsync(CancellationToken cancellationToken = default)
        {
            if (!_canRead)
            {
                throw new InvalidOperationException($"Characteristic {_name} ({_uuid}) does not support reading");
            }

            Debug.Log($"[macOS BLE] Reading characteristic {_name} ({_uuid})...");

            await Task.Delay(300, cancellationToken);

            _value = _uuid.ToLower() switch
            {
                "00002a00-0000-1000-8000-00805f9b34fb" => Encoding.UTF8.GetBytes("macOS Test Device"),
                "00002a01-0000-1000-8000-00805f9b34fb" => BitConverter.GetBytes((ushort)0x0080), // Generic Computer
                "00002a29-0000-1000-8000-00805f9b34fb" => Encoding.UTF8.GetBytes("Unity Technologies"),
                "00002a24-0000-1000-8000-00805f9b34fb" => Encoding.UTF8.GetBytes("UnityBLE macOS"),
                "00002a25-0000-1000-8000-00805f9b34fb" => Encoding.UTF8.GetBytes("SN001"),
                _ => Encoding.UTF8.GetBytes("Mock Value")
            };

            Debug.Log($"[macOS BLE] Read {_value.Length} bytes from characteristic {_name}");

            return _value;
        }

        public async Task WriteAsync(byte[] data, CancellationToken cancellationToken = default)
        {
            if (!_canWrite)
            {
                throw new InvalidOperationException($"Characteristic {_name} ({_uuid}) does not support writing");
            }

            Debug.Log($"[macOS BLE] Writing {data.Length} bytes to characteristic {_name} ({_uuid})...");

            await Task.Delay(300, cancellationToken);

            _value = new byte[data.Length];
            Array.Copy(data, _value, data.Length);

            Debug.Log($"[macOS BLE] Successfully wrote {data.Length} bytes to characteristic {_name}");
        }

        public Task StartNotificationsAsync(Action<byte[]> onNotification, CancellationToken cancellationToken = default)
        {
            if (!_canNotify)
            {
                throw new InvalidOperationException($"Characteristic {_name} ({_uuid}) does not support notifications");
            }

            Debug.Log($"[macOS BLE] Starting notifications for characteristic {_name} ({_uuid})");

            return Task.CompletedTask;
        }

        public Task StopNotificationsAsync(CancellationToken cancellationToken = default)
        {
            if (!_canNotify)
            {
                throw new InvalidOperationException($"Characteristic {_name} ({_uuid}) does not support notifications");
            }

            Debug.Log($"[macOS BLE] Stopping notifications for characteristic {_name} ({_uuid})");

            return Task.CompletedTask;
        }

        public override string ToString()
        {
            return $"MacOSBleCharacteristic: {_name} ({_uuid}) R:{_canRead} W:{_canWrite} N:{_canNotify}";
        }

        public Task WriteAsync(byte[] data, bool withResponse, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task SubscribeAsync(Action<byte[]> onValueChanged, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task UnsubscribeAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}