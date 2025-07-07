using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityBLE
{
    public abstract class UniversalBleDevice : IBleDevice
    {
        // BLE Standard UUIDs
        private const string GENERIC_ACCESS_SERVICE_UUID = "00001800-0000-1000-8000-00805f9b34fb";
        private const string SERVICE_CHANGED_CHARACTERISTIC_UUID = "00002a05-0000-1000-8000-00805f9b34fb";
        private const string BATTERY_SERVICE_UUID = "0000180f-0000-1000-8000-00805f9b34fb";
        private const string BATTERY_LEVEL_CHARACTERISTIC_UUID = "00002a19-0000-1000-8000-00805f9b34fb";

        public string Name { get; internal set; }
        public string Address { get; internal set; }
        public int Rssi { get; internal set; }
        public bool IsConnectable { get; internal set; }
        public int TxPower { get; internal set; }
        public string AdvertisingData { get; internal set; }
        public bool IsConnected => _isConnected;

        private bool _isConnected = false;
        internal List<IBleService> _services = new List<IBleService>();

        public IEnumerable<IBleService> Services => _services;

        public BleDeviceEvents Events { get; } = new BleDeviceEvents();

        internal abstract Task<IBleDevice> ExecuteConnectAsync(CancellationToken cancellationToken);
        internal abstract Task<bool> ExecuteDisconnectAsync(CancellationToken cancellationToken);
        internal abstract Task<IReadOnlyList<IBleService>> ExecuteGetServicesCommandAsync(CancellationToken cancellationToken = default);

        internal abstract void EndSubscribeCharacteristic();

        internal abstract Task AutoSubscribeToCharacteristicAsync(IBleCharacteristic characteristic, CancellationToken cancellationToken = default);

        public async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            if (_isConnected)
            {
                Debug.Log($"[UnityBLE] Device {Address} is already connected");
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();

            var device = await ExecuteConnectAsync(cancellationToken);
            if (device == null)
            {
                Debug.LogError($"[UnityBLE] Failed to connect to device {Address}");
                throw new InvalidOperationException($"Failed to connect to device {Address}");
            }

            _services = device.Services.ToList();
            _isConnected = true;

            // Automatically discover services and subscribe to notification-capable characteristics
            await DiscoverServicesAndAutoSubscribeAsync(cancellationToken);

            Events.RaiseConnected(this);
            Debug.Log($"[UnityBLE] Device {Address} connected successfully");
        }

        public async Task DisconnectAsync(CancellationToken cancellationToken = default)
        {
            if (!_isConnected)
            {
                Debug.Log($"[UnityBLE] Device {Address} is not connected");
                return;
            }

            // Clean up all subscriptions before disconnecting

            if (await ExecuteDisconnectAsync(cancellationToken))
            {
                _isConnected = false;
                _services.Clear();
                Events.RaiseDisconnected(this);
                EndSubscribeCharacteristic();
                Debug.Log($"[UnityBLE] Device {Address} disconnected successfully");
            }
        }

        public async Task ReloadServicesAsync(CancellationToken cancellationToken = default)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException($"Device {Address} is not connected. Connect first before getting services.");
            }

            var services = await ExecuteGetServicesCommandAsync(cancellationToken);
            _services = new List<IBleService>(services);
            Debug.Log($"[UnityBLE] Reloaded {services.Count} services for device {Address}");
        }

        internal void OnCharacteristicValueReceived(string characteristicJson, string valueHex)
        {
            Debug.Log($"[UnityBLE] Characteristic value received: {characteristicJson} with value {valueHex}");
            // Convert hex string back to byte array
            byte[] data = Array.Empty<byte>();
            if (!string.IsNullOrEmpty(valueHex))
            {
                data = HexStringToByteArray(valueHex);
            }

            // If this is a notification and we're subscribed
            var characteristicUUID = "";
            if (characteristicUUID.Equals(SERVICE_CHANGED_CHARACTERISTIC_UUID))
            {
                ReloadServicesAsync().ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        Debug.LogError($"[UnitBLE] Failed to reload services after Service Changed: {t.Exception?.Message}");
                    }
                    else
                    {
                        Debug.Log($"[UnitBLE] Successfully reloaded services after Service Changed");
                        Events.RaiseServicesChanged(this);
                    }
                }, TaskScheduler.Current);
            }
            else
            {
                Events.RaiseDataReceived(characteristicUUID, data);
            }
        }

        private static byte[] HexStringToByteArray(string hex)
        {
            if (string.IsNullOrEmpty(hex) || hex.Length % 2 != 0)
                return new byte[0];

            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < hex.Length; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
            return bytes;
        }

        private async Task DiscoverServicesAndAutoSubscribeAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                Debug.Log($"[UnityBLE] Starting automatic service discovery and subscription for device {Address}");

                // Reload services to ensure we have the latest service list
                await ReloadServicesAsync(cancellationToken);

                // Collect all notification-capable characteristics
                var notificationCharacteristics = new List<IBleCharacteristic>();

                foreach (var service in _services)
                {
                    try
                    {
                        // Get characteristics for this service
                        var characteristics = await service.GetCharacteristicsAsync(cancellationToken);

                        // Find characteristics that support notifications
                        foreach (var characteristic in characteristics)
                        {
                            if (characteristic.CanNotify)
                            {
                                notificationCharacteristics.Add(characteristic);
                                Debug.Log($"[UnityBLE] Found notification-capable characteristic: {characteristic.Uuid} in service {service.Uuid}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[UnityBLE] Failed to get characteristics for service {service.Uuid}: {ex.Message}");
                    }
                }

                // Subscribe to all notification-capable characteristics
                foreach (var characteristic in notificationCharacteristics)
                {
                    try
                    {
                        await AutoSubscribeToCharacteristicAsync(characteristic, cancellationToken);
                        Debug.Log($"[UnityBLE] Auto-subscribed to characteristic: {characteristic.Uuid}");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[UnityBLE] Failed to auto-subscribe to characteristic {characteristic.Uuid}: {ex.Message}");
                    }
                }

                Debug.Log($"[UnityBLE] Automatic subscription completed. Subscribed to {notificationCharacteristics.Count} characteristics");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UnityBLE] Error during automatic service discovery and subscription: {ex.Message}");
            }
        }

        public override string ToString()
        {
            return $"Peripheral: {Name} ({Address}) RSSI: {Rssi} Connected: {IsConnected}";
        }
    }
}