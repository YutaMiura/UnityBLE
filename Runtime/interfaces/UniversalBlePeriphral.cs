using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityBLE
{
    public abstract class UniversalBlePeripheral : IBlePeripheral
    {
        public string Name { get; internal set; }
        public string UUID { get; internal set; }
        public int Rssi { get; internal set; }
        public bool IsConnectable { get; internal set; }
        public int TxPower { get; internal set; }
        public string AdvertisingData { get; internal set; }
        public bool IsConnected => _isConnected;

        private bool _isConnected = false;
        internal ConcurrentDictionary<string, IBleService> _services = new();

        public event IBlePeripheral.ConnectionStatusChangedDelegate OnConnectionStatusChanged;
        public event IBlePeripheral.OnServiceDiscoveredDelegate OnServiceDiscovered;

        public IEnumerable<IBleService> Services => _services.Values;

        internal abstract Task<IBlePeripheral> ExecuteConnectAsync(CancellationToken cancellationToken);
        internal abstract Task<bool> ExecuteDisconnectAsync(CancellationToken cancellationToken);

        protected abstract void DiscoverServices();

        protected UniversalBlePeripheral()
        {
            Debug.Log($"[UnityBLE] Initializing UniversalBlePeripheral for {UUID}");
        }

        public async Task ConnectAsync(CancellationToken cancellationToken = default)
        {

            if (_isConnected)
            {
                Debug.Log($"[UnityBLE] Device {UUID} is already connected");
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                BleDeviceEvents.OnConnected += OnConnected;
                BleDeviceEvents.OnDisconnected += OnDisconnected;
                BleDeviceEvents.OnServicesDiscovered += OnServiceDiscoveredHandler;
                var device = await ExecuteConnectAsync(cancellationToken);
                if (device == null)
                {
                    Debug.LogError($"[UnityBLE] Failed to connect to device {UUID}");
                    throw new InvalidOperationException($"Failed to connect to device {UUID}");
                }

                // Update connection state immediately after successful connection
                // Note: For iOS, the connection state may already be set by the native callback handler
                if (!_isConnected)
                {
                    _isConnected = true;
                }
            }
            catch (Exception ex)
            {
                // Ensure connection state is false if connection fails
                _isConnected = false;
                _services.Clear();
                Debug.LogError($"[UnityBLE] Connection failed for device {UUID}: {ex.Message}");
                throw;
            }
        }

        private void OnDisconnected(string deviceUuid)
        {
            if (deviceUuid != UUID)
            {
                Debug.LogWarning($"Disconnected device UUID {deviceUuid} does not match current device UUID {UUID}");
                return;
            }
            _isConnected = false;
            Debug.Log($"[UnityBLE] Device {UUID} connection state updated: {_isConnected}");
            OnConnectionStatusChanged?.Invoke(this, _isConnected);
        }

        private void OnConnected(IBlePeripheral device)
        {
            Debug.Log($"[UnityBLE] Device {device.UUID} connected");
            if (device.UUID != UUID)
            {
                Debug.LogWarning($"[UnityBLE] Connected device UUID {device.UUID} does not match this device UUID {UUID}, ignoring.");
                return;
            }

            _isConnected = true;
            Debug.Log($"[UnityBLE] Device {UUID} connection state updated: {_isConnected}");
            OnConnectionStatusChanged?.Invoke(this, _isConnected);
            DiscoverServices();
        }

        private void OnServiceDiscoveredHandler(IBleService service)
        {
            if (service.PeripheralUUID != UUID)
            {
                Debug.LogWarning($"[UnityBLE] Service {service.Uuid} does not belong to device {UUID}, skipping.");
                return;
            }

            if (_services.TryAdd(service.Uuid, service))
            {
                Debug.Log($"[UnityBLE] Discovered {service}");
                OnServiceDiscovered?.Invoke(service);
            }
            else
            {
                Debug.LogWarning($"[UnityBLE] Service {service.Uuid} already exists for device {UUID}, skipping.");
            }
        }

        public async Task DisconnectAsync(CancellationToken cancellationToken = default)
        {
            if (!_isConnected)
            {
                Debug.Log($"[UnityBLE] Device {UUID} is not connected");
                return;
            }

            // Clean up all subscriptions before disconnecting

            if (await ExecuteDisconnectAsync(cancellationToken))
            {
                _isConnected = false;
                foreach (var service in _services.Values)
                {
                    service.Dispose();
                }
                _services.Clear();
                BleDeviceEvents.OnServicesDiscovered -= OnServiceDiscoveredHandler;
                BleDeviceEvents.OnConnected -= OnConnected;
                BleDeviceEvents.OnDisconnected -= OnDisconnected;
                Debug.Log($"[UnityBLE] Device {UUID} disconnected successfully");
            }
        }

        public override string ToString()
        {
            return $"Peripheral: {Name} ({UUID}) RSSI: {Rssi} Connected: {IsConnected}";
        }

        public void Dispose()
        {
            BleDeviceEvents.OnServicesDiscovered -= OnServiceDiscoveredHandler;
            BleDeviceEvents.OnConnected -= OnConnected;
            BleDeviceEvents.OnDisconnected -= OnDisconnected;
        }
    }
}