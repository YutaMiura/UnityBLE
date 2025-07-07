using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityBLE.macOS
{
    /// <summary>
    /// macOS Unity Editor implementation of IBleDevice for testing purposes.
    /// </summary>
    public class MacOSBleDevice : UniversalBleDevice
    {


        // Subscription management
        private readonly Dictionary<string, MacOSSubscribeCharacteristicCommand> _subscribeCommands = new();
        private readonly Dictionary<string, MacOSUnsubscribeCharacteristicCommand> _unsubscribeCommands = new();
        private readonly Dictionary<string, MacOSSubscribeCharacteristicCommand> _autoSubscribeCommands = new();

        public MacOSBleDevice(string name, string address, int rssi, bool isConnectable, int txPower, string advertisingData)
        {
            Name = name;
            Address = address;
            Rssi = rssi;
            IsConnectable = isConnectable;
            TxPower = txPower;
            AdvertisingData = advertisingData;
        }

        internal override Task<IBleDevice> ExecuteConnectAsync(CancellationToken cancellationToken)
        {
            return new MacOSConnectDeviceCommand(this).ExecuteAsync(cancellationToken);
        }

        internal override Task<bool> ExecuteDisconnectAsync(CancellationToken cancellationToken)
        {
            return new MacOSDisconnectDeviceCommand().ExecuteAsync(this, cancellationToken);
        }

        internal override Task<IReadOnlyList<IBleService>> ExecuteGetServicesCommandAsync(CancellationToken cancellationToken = default)
        {
            var services = new MacOSGetServicesCommand().Execute(this, cancellationToken);
            return Task.FromResult<IReadOnlyList<IBleService>>(services);
        }

        internal override void EndSubscribeCharacteristic()
        {
            MacOSBleNativePlugin.OnCharacteristicValue -= OnCharacteristicValueReceived;

            // Clean up auto-subscribed characteristics
            foreach (var command in _autoSubscribeCommands.Values)
            {
                try
                {
                    command.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[macOS BLE] Error disposing auto-subscribe command: {ex.Message}");
                }
            }
            _autoSubscribeCommands.Clear();
        }

        internal override async Task AutoSubscribeToCharacteristicAsync(IBleCharacteristic characteristic, CancellationToken cancellationToken = default)
        {
            if (characteristic == null)
            {
                Debug.LogWarning("[macOS BLE] Cannot auto-subscribe to null characteristic");
                return;
            }

            var characteristicKey = characteristic.Uuid;

            // Check if already auto-subscribed
            if (_autoSubscribeCommands.ContainsKey(characteristicKey))
            {
                Debug.LogWarning($"[macOS BLE] Already auto-subscribed to characteristic {characteristicKey}");
                return;
            }

            // Note: Auto-subscribe will work alongside manual subscribe
            // The native plugin handles multiple subscriptions to the same characteristic gracefully

            try
            {
                // Find the service UUID for this characteristic
                string serviceUuid = null;
                foreach (var service in Services)
                {
                    var serviceCharacteristics = await service.GetCharacteristicsAsync(cancellationToken);
                    if (serviceCharacteristics.Any(c => c.Uuid == characteristic.Uuid))
                    {
                        serviceUuid = service.Uuid;
                        break;
                    }
                }

                if (serviceUuid == null)
                {
                    Debug.LogError($"[macOS BLE] Could not find service for characteristic {characteristicKey}");
                    return;
                }

                // Create and execute subscription command
                var subscribeCommand = new MacOSSubscribeCharacteristicCommand(Address, serviceUuid, characteristic.Uuid);

                await subscribeCommand.ExecuteAsync(data =>
                {
                    // Forward data to the UniversalBleDevice event system
                    Events.RaiseDataReceived(characteristic.Uuid, data);
                }, cancellationToken);

                // Store the command for cleanup
                _autoSubscribeCommands[characteristicKey] = subscribeCommand;

                Debug.Log($"[macOS BLE] Successfully auto-subscribed to characteristic {characteristicKey}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[macOS BLE] Failed to auto-subscribe to characteristic {characteristicKey}: {ex.Message}");
                throw;
            }
        }
    }
}