using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace UnityBLE.apple
{
    /// <summary>
    /// macOS Unity Editor implementation of IBleDevice for testing purposes.
    /// </summary>
    public class AppleBlePeripheral : UniversalBlePeripheral
    {
        public AppleBlePeripheral(PeripheralDTO dto)
        {
            Name = dto.name ?? "Unknown Device";
            UUID = dto.uuid ?? "00:00:00:00:00:00";
            Rssi = dto.rssi;
            IsConnectable = true; // Assume connectable for testing
            TxPower = 0; // Default value for testing
            AdvertisingData = string.Empty; // No advertising data in this context

            // Initialize services and characteristics if needed
            _services = new();
        }

        public AppleBlePeripheral(string name, string uuid, int rssi, bool isConnectable, int txPower, string advertisingData)
        {
            Name = name;
            UUID = uuid;
            Rssi = rssi;
            IsConnectable = isConnectable;
            TxPower = txPower;
            AdvertisingData = advertisingData;
        }

        protected override void DiscoverServices()
        {
            AppleBleNativePlugin.StartDiscoveryService(this);
        }

        internal override Task<IBlePeripheral> ExecuteConnectAsync(CancellationToken cancellationToken)
        {
            return new AppleConnectDeviceCommand(this).ExecuteAsync();
        }

        internal override Task<bool> ExecuteDisconnectAsync(CancellationToken cancellationToken)
        {
            return new AppleDisconnectDeviceCommand(this).ExecuteAsync(this, cancellationToken);
        }
    }
}