using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityBLE.iOS
{
    /// <summary>
    /// iOS implementation of IBleDevice for real iOS devices.
    /// </summary>
    public class iOSBleDevice : IBleDevice
    {
        // BLE Standard UUIDs
        private const string GENERIC_ACCESS_SERVICE_UUID = "00001800-0000-1000-8000-00805f9b34fb";
        private const string SERVICE_CHANGED_CHARACTERISTIC_UUID = "00002a05-0000-1000-8000-00805f9b34fb";
        private const string BATTERY_SERVICE_UUID = "0000180f-0000-1000-8000-00805f9b34fb";
        private const string BATTERY_LEVEL_CHARACTERISTIC_UUID = "00002a19-0000-1000-8000-00805f9b34fb";

        private readonly string _name;
        private readonly string _address;
        private readonly int _rssi;
        private readonly bool _isConnectable;
        private readonly int _txPower;
        private readonly string _advertisingData;
        private bool _isConnected = false;
        private List<IBleService> _services = new();

        // Subscription management
        private readonly Dictionary<string, iOSSubscribeCharacteristicCommand> _subscribeCommands = new();
        private readonly Dictionary<string, iOSUnsubscribeCharacteristicCommand> _unsubscribeCommands = new();

        public string Name => _name;
        public string Address => _address;
        public int Rssi => _rssi;
        public bool IsConnectable => _isConnectable;
        public int TxPower => _txPower;
        public string AdvertisingData => _advertisingData;
        public bool IsConnected => _isConnected;

        public IEnumerable<IBleService> Services => _services;

        public BleDeviceEvents Events { get; } = new BleDeviceEvents();

        public iOSBleDevice(string name, string address)
        {
            _name = name;
            _address = address;
            _rssi = -50;
            _isConnectable = true;
            _txPower = 4;
            _advertisingData = "iOS advertising data";
        }

        public iOSBleDevice(string name, string address, int rssi, bool isConnectable, int txPower, string advertisingData)
        {
            _name = name;
            _address = address;
            _rssi = rssi;
            _isConnectable = isConnectable;
            _txPower = txPower;
            _advertisingData = advertisingData;
        }

        public async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            if (_isConnected)
            {
                Debug.Log($"[iOS BLE] Device {_address} is already connected");
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();

            var connectedDevice = await new iOSConnectDeviceCommand().ExecuteAsync(_address, cancellationToken);
            _isConnected = true;
            Events.RaiseConnected(this);
            Debug.Log($"[iOS BLE] Device {_address} connected successfully");

            // Auto-subscribe to essential characteristics after connection
            try
            {
                await SubscribeToEssentialCharacteristicsAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[iOS BLE] Failed to auto-subscribe to essential characteristics: {ex.Message}");
            }
        }

        public async Task DisconnectAsync(CancellationToken cancellationToken = default)
        {
            if (!_isConnected)
            {
                Debug.Log($"[iOS BLE] Device {_address} is not connected");
                return;
            }

            // Clean up all subscriptions before disconnecting
            await CleanupSubscriptionsAsync();

            if (await new iOSDisconnectDeviceCommand().ExecuteAsync(this, cancellationToken))
            {
                _isConnected = false;
                _services.Clear();
                Events.RaiseDisconnected(this);
                Debug.Log($"[iOS BLE] Device {_address} disconnected successfully");
            }
        }

        public async Task ReloadServicesAsync(CancellationToken cancellationToken = default)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException($"Device {_address} is not connected. Connect first before getting services.");
            }

            var getServicesCommand = new iOSGetServicesCommand();
            var services = await getServicesCommand.ExecuteAsync(this, cancellationToken);

            _services.Clear();
            _services.AddRange(services);

            Debug.Log($"[iOS BLE] Reloaded {services.Count} services for device {_address}");
        }

        public async Task SubscribeToEssentialCharacteristicsAsync(CancellationToken cancellationToken = default)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException($"Device {_address} is not connected");
            }

            Debug.Log($"[iOS BLE] Subscribing to essential characteristics for device {_address}");

            try
            {
                // Subscribe to Service Changed characteristic
                await SubscribeToCharacteristicAsync(GENERIC_ACCESS_SERVICE_UUID, SERVICE_CHANGED_CHARACTERISTIC_UUID, cancellationToken);
                Debug.Log($"[iOS BLE] Subscribed to Service Changed characteristic");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[iOS BLE] Failed to subscribe to Service Changed: {ex.Message}");
            }

            // Optionally subscribe to Battery Level if available
            try
            {
                await SubscribeToCharacteristicAsync(BATTERY_SERVICE_UUID, BATTERY_LEVEL_CHARACTERISTIC_UUID, cancellationToken);
                Debug.Log($"[iOS BLE] Subscribed to Battery Level characteristic");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[iOS BLE] Battery service not available or failed to subscribe: {ex.Message}");
            }
        }

        public async Task SubscribeToCharacteristicAsync(string serviceUuid, string characteristicUuid, CancellationToken cancellationToken = default)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException($"Device {_address} is not connected");
            }

            var subscriptionKey = $"{serviceUuid}:{characteristicUuid}";

            if (_subscribeCommands.ContainsKey(subscriptionKey))
            {
                Debug.LogWarning($"[iOS BLE] Already subscribed to characteristic {characteristicUuid}");
                return;
            }

            var subscribeCommand = new iOSSubscribeCharacteristicCommand(_address, serviceUuid, characteristicUuid);
            var unsubscribeCommand = new iOSUnsubscribeCharacteristicCommand(_address, serviceUuid, characteristicUuid);

            await subscribeCommand.ExecuteAsync(data => OnCharacteristicNotificationReceived(serviceUuid, characteristicUuid, data), cancellationToken);

            _subscribeCommands[subscriptionKey] = subscribeCommand;
            _unsubscribeCommands[subscriptionKey] = unsubscribeCommand;

            Debug.Log($"[iOS BLE] Successfully subscribed to characteristic {characteristicUuid}");
        }

        public async Task UnsubscribeFromCharacteristicAsync(string serviceUuid, string characteristicUuid, CancellationToken cancellationToken = default)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException($"Device {_address} is not connected");
            }

            var subscriptionKey = $"{serviceUuid}:{characteristicUuid}";

            if (!_unsubscribeCommands.TryGetValue(subscriptionKey, out var unsubscribeCommand))
            {
                Debug.LogWarning($"[iOS BLE] Not subscribed to characteristic {characteristicUuid}");
                return;
            }

            await unsubscribeCommand.ExecuteAsync(cancellationToken);

            // Clean up command references
            _subscribeCommands.Remove(subscriptionKey);
            _unsubscribeCommands.Remove(subscriptionKey);

            Debug.Log($"[iOS BLE] Successfully unsubscribed from characteristic {characteristicUuid}");
        }

        private void OnCharacteristicNotificationReceived(string serviceUuid, string characteristicUuid, byte[] data)
        {
            var notification = new BleCharacteristicNotification(serviceUuid, characteristicUuid, data);
            Events.RaiseCharacteristicNotification(this, notification);

            // Special handling for Service Changed characteristic
            if (characteristicUuid.Equals(SERVICE_CHANGED_CHARACTERISTIC_UUID, StringComparison.OrdinalIgnoreCase))
            {
                Debug.Log($"[iOS BLE] Service Changed notification received from device {_address}");
                Events.RaiseServicesChanged(this);

                // Automatically reload services when Service Changed is received
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(1000); // Brief delay before reloading
                        await ReloadServicesAsync();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[iOS BLE] Failed to reload services after Service Changed: {ex.Message}");
                    }
                });
            }
        }

        private async Task CleanupSubscriptionsAsync()
        {
            Debug.Log($"[iOS BLE] Cleaning up {_subscribeCommands.Count} subscriptions for device {_address}");

            var cleanupTasks = new List<Task>();

            foreach (var kvp in _unsubscribeCommands.ToList())
            {
                cleanupTasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        await kvp.Value.ExecuteAsync();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[iOS BLE] Failed to cleanup subscription {kvp.Key}: {ex.Message}");
                    }
                }));
            }

            await Task.WhenAll(cleanupTasks);

            _subscribeCommands.Clear();
            _unsubscribeCommands.Clear();
        }

        public override string ToString()
        {
            return $"iOSBleDevice: {_name} ({_address}) RSSI: {_rssi} Connected: {_isConnected}";
        }
    }
}