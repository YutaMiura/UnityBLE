using System.Threading;
using System.Threading.Tasks;

namespace UnityBLE.windows
{
    /// <summary>
    /// Windows (C++/WinRT) implementation of a BLE peripheral.
    /// </summary>
    public class WindowsBlePeripheral : UniversalBlePeripheral
    {
        internal WindowsBlePeripheral(PeripheralDTO dto)
        {
            Name = dto.name ?? "Unknown Device";
            UUID = dto.uuid ?? "00:00:00:00:00:00";
            Rssi = dto.rssi;
            IsConnectable = true;
            TxPower = 0;
            AdvertisingData = string.Empty;

            _services = new();
        }

        public WindowsBlePeripheral(string name, string uuid, int rssi, bool isConnectable, int txPower, string advertisingData)
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
            WindowsBleNativePlugin.StartDiscoveryService(this);
        }

        internal override Task<IBlePeripheral> ExecuteConnectAsync(CancellationToken cancellationToken)
        {
            return new WindowsConnectDeviceCommand(this).ExecuteAsync(cancellationToken);
        }

        internal override Task<bool> ExecuteDisconnectAsync(CancellationToken cancellationToken)
        {
            return new WindowsDisconnectDeviceCommand(this).ExecuteAsync(this, cancellationToken);
        }
    }
}
