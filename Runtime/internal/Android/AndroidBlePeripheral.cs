using System.Threading;
using System.Threading.Tasks;

namespace UnityBLE.Android
{
    public class AndroidBlePeripheral : UniversalBlePeripheral
    {
        internal AndroidBlePeripheral(PeripheralDTO dto)
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

        internal AndroidBlePeripheral(
            string name,
            string uuid,
            int rssi,
            bool isConnectable,
            int txPower,
            string advertisingData)
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
            NativeFacade.Instance.DiscoverServices(this);
        }

        internal override Task<IBlePeripheral> ExecuteConnectAsync(CancellationToken cancellationToken)
        {
            return NativeFacade.Instance.ConnectAsync(this);
        }

        internal override Task<bool> ExecuteDisconnectAsync(CancellationToken cancellationToken)
        {
            return NativeFacade.Instance.DisconnectAsync(this);
        }
    }
}