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

        // Base64-encoded Manufacturer Specific Data of the advertisement.
        // See PeripheralDTO.manufacturerData for the per-platform byte layout.
        // Empty string when the advertisement has no MSD.
        public string ManufacturerData { get; internal set; }
        public bool IsConnected => _isConnected;

        // The native connect has no timeout of its own on any platform (Android's direct
        // connectGatt eventually fails with 133, but iOS/CoreBluetooth keeps a pending
        // connection alive indefinitely). The library therefore always bounds ConnectAsync so
        // a never-establishing link cannot hang the caller forever.
        private static readonly TimeSpan DefaultConnectTimeout = TimeSpan.FromSeconds(15);

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

        // Two overloads (rather than one method with an optional TimeSpan?) so expression-tree
        // call sites - e.g. Moq's Setup(m => m.ConnectAsync(It.IsAny<CancellationToken>())) -
        // keep compiling: an expression tree may not omit an optional argument (CS0854).
        public Task ConnectAsync(CancellationToken cancellationToken = default)
            => ConnectAsync(cancellationToken, DefaultConnectTimeout);

        public async Task ConnectAsync(CancellationToken cancellationToken, TimeSpan timeout)
        {

            if (_isConnected)
            {
                Debug.Log($"[UnityBLE] Device {UUID} is already connected");
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Always bound the connect. The linked token cancels ExecuteConnectAsync on either
            // a caller cancellation or the library timeout; each platform command honors it, so
            // a never-establishing link is aborted instead of hanging forever.
            using var timeoutCts = new CancellationTokenSource(timeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            try
            {
                BleDeviceEvents.OnConnected += OnConnected;
                BleDeviceEvents.OnDisconnected += OnDisconnected;
                BleDeviceEvents.OnServicesDiscovered += OnServiceDiscoveredHandler;
                var device = await ExecuteConnectAsync(linkedCts.Token);
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
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                // The library timeout fired (not a caller cancellation): surface it as a distinct
                // TimeoutException so callers can tell "took too long" from "was cancelled".
                Cleanup();
                Debug.LogError($"[UnityBLE] Connecting to device {UUID} timed out after {timeout.TotalSeconds}s.");
                throw new TimeoutException($"Connecting to device {UUID} timed out after {timeout.TotalSeconds}s.");
            }
            catch (Exception ex)
            {
                Cleanup();
                Debug.LogError($"[UnityBLE] Connection failed for device {UUID}: {ex.Message}");
                throw;
            }

            void Cleanup()
            {
                // Ensure connection state is false if connection fails
                _isConnected = false;
                _services.Clear();
                BleDeviceEvents.OnServicesDiscovered -= OnServiceDiscoveredHandler;
                BleDeviceEvents.OnConnected -= OnConnected;
                BleDeviceEvents.OnDisconnected -= OnDisconnected;
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

            // Release everything tied to the dropped link. Without this, an unexpected disconnect
            // (e.g. a link-supervision timeout / status 8) leaves this peripheral subscribed to the
            // static BleDeviceEvents forever - a zombie that keeps receiving global callbacks for
            // other devices (logged as "does not match current device UUID") and is never cleaned
            // up, because a later DisconnectAsync early-returns on !_isConnected.
            foreach (var service in _services.Values)
            {
                service.Dispose();
            }
            _services.Clear();
            BleDeviceEvents.OnServicesDiscovered -= OnServiceDiscoveredHandler;
            BleDeviceEvents.OnConnected -= OnConnected;
            BleDeviceEvents.OnDisconnected -= OnDisconnected;
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
