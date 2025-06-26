using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityBLE.Android;
using UnityEngine;

namespace UnityBLE
{
    /// <summary>
    /// Android implementation of IBleDevice.
    /// </summary>
    public class AndroidBleDevice : IBleDevice
    {
        private readonly string _name;
        private readonly string _address;
        private readonly int _rssi;
        private readonly bool _isConnectable;
        private readonly int _txPower;
        private readonly string _advertisingData;
        private bool _isConnected = false;
        private List<IBleService> _services = new();
        private CancellationTokenRegistration _connectionCancellationRegistration;

        public string Name => _name;
        public string Address => _address;
        public int Rssi => _rssi;
        public bool IsConnectable => _isConnectable;
        public int TxPower => _txPower;
        public string AdvertisingData => _advertisingData;
        public bool IsConnected => _isConnected;

        public IEnumerable<IBleService> Services => _services;

        public AndroidBleDevice(string name, string address)
        {
            _name = name;
            _address = address;
            _rssi = 0;
            _isConnectable = true;
            _txPower = 0;
            _advertisingData = "";
        }

        public AndroidBleDevice(string name, string address, int rssi, bool isConnectable, int txPower, string advertisingData)
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
                Debug.Log($"Device {_address} is already connected");
                return;
            }

            // Check if already cancelled
            cancellationToken.ThrowIfCancellationRequested();

            await new AndroidConnectDeviceCommand().ExecuteAsync(_address, cancellationToken);
            _isConnected = true;
            Debug.Log($"Device {_address} connected successfully");
        }

        public Task<bool> DisconnectAsync(CancellationToken cancellationToken = default)
        {
            if (!_isConnected)
            {
                Debug.Log($"Device {_address} is not connected");
                return Task.FromResult(true);
            }

            return new AndroidDisconnectDeviceCommand().ExecuteAsync(this, cancellationToken);
        }

        public async Task<IReadOnlyList<IBleService>> GetServicesAsync(CancellationToken cancellationToken = default)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException($"Device {_address} is not connected. Connect first before getting services.");
            }

            // Use the new GetServicesCommand
            var getServicesCommand = new AndroidGetServicesCommand();
            var services = await getServicesCommand.ExecuteAsync(this, cancellationToken);

            // Update internal services list
            _services.Clear();
            _services.AddRange(services);

            return services;
        }

        public override string ToString()
        {
            return $"AndroidBleDevice: {_name} ({_address}) RSSI: {_rssi} Connected: {_isConnected}";
        }

        Task IBleDevice.DisconnectAsync(CancellationToken cancellationToken)
        {
            return DisconnectAsync(cancellationToken);
        }

        // Clean up event subscriptions
        ~AndroidBleDevice()
        {
            // Clean up cancellation registrations
            _connectionCancellationRegistration.Dispose();
        }
    }
}