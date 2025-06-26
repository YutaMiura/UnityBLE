using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

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
        private readonly bool _canRead;
        private readonly bool _canWrite;
        private readonly bool _canNotify;

        public string Name => _name;
        public string Uuid => _uuid;
        public byte[] Value => _value;
        public bool CanRead => _canRead;
        public bool CanWrite => _canWrite;
        public bool CanNotify => _canNotify;

        public iOSBleCharacteristic(string name, string uuid, string deviceAddress, string serviceUuid)
        {
            _name = name;
            _uuid = uuid;
            _deviceAddress = deviceAddress;
            _serviceUuid = serviceUuid;
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

            Debug.Log($"[iOS BLE] Reading characteristic {_name} ({_uuid})...");

            try
            {
                // Initialize native plugin if not already done
                if (!iOSBleNativePlugin.Initialize())
                {
                    throw new InvalidOperationException("Failed to initialize iOS BLE native plugin");
                }

                // Call native read
                if (!iOSBleNativePlugin.ReadCharacteristic(_deviceAddress, _serviceUuid, _uuid))
                {
                    throw new InvalidOperationException($"Failed to start reading characteristic {_uuid}");
                }

                // Wait for characteristic value callback (this would be handled through events in real implementation)
                await Task.Delay(500, cancellationToken);

                // For now, return mock data similar to macOS implementation
                _value = _uuid.ToLower() switch
                {
                    "00002a00-0000-1000-8000-00805f9b34fb" => Encoding.UTF8.GetBytes("iOS Test Device"),
                    "00002a01-0000-1000-8000-00805f9b34fb" => BitConverter.GetBytes((ushort)0x0080), // Generic Computer
                    "00002a29-0000-1000-8000-00805f9b34fb" => Encoding.UTF8.GetBytes("Unity Technologies"),
                    "00002a24-0000-1000-8000-00805f9b34fb" => Encoding.UTF8.GetBytes("UnityBLE iOS"),
                    "00002a25-0000-1000-8000-00805f9b34fb" => Encoding.UTF8.GetBytes("SN001"),
                    _ => Encoding.UTF8.GetBytes("iOS Mock Value")
                };

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
            if (!_canWrite)
            {
                throw new InvalidOperationException($"Characteristic {_name} ({_uuid}) does not support writing");
            }

            Debug.Log($"[iOS BLE] Writing {data.Length} bytes to characteristic {_name} ({_uuid})...");

            try
            {
                // Initialize native plugin if not already done
                if (!iOSBleNativePlugin.Initialize())
                {
                    throw new InvalidOperationException("Failed to initialize iOS BLE native plugin");
                }

                // Call native write
                if (!iOSBleNativePlugin.WriteCharacteristic(_deviceAddress, _serviceUuid, _uuid, data))
                {
                    throw new InvalidOperationException($"Failed to write to characteristic {_uuid}");
                }

                await Task.Delay(300, cancellationToken);

                _value = new byte[data.Length];
                Array.Copy(data, _value, data.Length);

                Debug.Log($"[iOS BLE] Successfully wrote {data.Length} bytes to characteristic {_name}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[iOS BLE] Error writing characteristic {_name}: {ex.Message}");
                throw;
            }
        }

        public Task StartNotificationsAsync(Action<byte[]> onNotification, CancellationToken cancellationToken = default)
        {
            if (!_canNotify)
            {
                throw new InvalidOperationException($"Characteristic {_name} ({_uuid}) does not support notifications");
            }

            Debug.Log($"[iOS BLE] Starting notifications for characteristic {_name} ({_uuid})");

            // TODO: Implement native notification subscription through events
            return Task.CompletedTask;
        }

        public Task StopNotificationsAsync(CancellationToken cancellationToken = default)
        {
            if (!_canNotify)
            {
                throw new InvalidOperationException($"Characteristic {_name} ({_uuid}) does not support notifications");
            }

            Debug.Log($"[iOS BLE] Stopping notifications for characteristic {_name} ({_uuid})");

            // TODO: Implement native notification unsubscription
            return Task.CompletedTask;
        }

        public override string ToString()
        {
            return $"iOSBleCharacteristic: {_name} ({_uuid}) R:{_canRead} W:{_canWrite} N:{_canNotify}";
        }

        public Task WriteAsync(byte[] data, bool withResponse, CancellationToken cancellationToken = default)
        {
            // iOS Core Bluetooth automatically handles write type based on characteristic properties
            return WriteAsync(data, cancellationToken);
        }

        public Task SubscribeAsync(Action<byte[]> onValueChanged, CancellationToken cancellationToken = default)
        {
            return StartNotificationsAsync(onValueChanged, cancellationToken);
        }

        public Task UnsubscribeAsync(CancellationToken cancellationToken = default)
        {
            return StopNotificationsAsync(cancellationToken);
        }
    }
}