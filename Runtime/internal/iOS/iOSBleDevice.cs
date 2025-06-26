using System;
using System.Collections.Generic;
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
        private readonly string _name;
        private readonly string _address;
        private readonly int _rssi;
        private readonly bool _isConnectable;
        private readonly int _txPower;
        private readonly string _advertisingData;
        private bool _isConnected = false;
        private List<IBleService> _services = new();

        public string Name => _name;
        public string Address => _address;
        public int Rssi => _rssi;
        public bool IsConnectable => _isConnectable;
        public int TxPower => _txPower;
        public string AdvertisingData => _advertisingData;
        public bool IsConnected => _isConnected;

        public IEnumerable<IBleService> Services => _services;

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
            Debug.Log($"[iOS BLE] Device {_address} connected successfully");
        }

        public async Task DisconnectAsync(CancellationToken cancellationToken = default)
        {
            if (!_isConnected)
            {
                Debug.Log($"[iOS BLE] Device {_address} is not connected");
                return;
            }

            await new iOSDisconnectDeviceCommand().ExecuteAsync(this, cancellationToken);
            _isConnected = false;
            _services.Clear();
            Debug.Log($"[iOS BLE] Device {_address} disconnected successfully");
        }

        public async Task<IReadOnlyList<IBleService>> GetServicesAsync(CancellationToken cancellationToken = default)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException($"Device {_address} is not connected. Connect first before getting services.");
            }

            var getServicesCommand = new iOSGetServicesCommand();
            var services = await getServicesCommand.ExecuteAsync(this, cancellationToken);

            _services.Clear();
            _services.AddRange(services);

            return services;
        }

        public override string ToString()
        {
            return $"iOSBleDevice: {_name} ({_address}) RSSI: {_rssi} Connected: {_isConnected}";
        }
    }
}