using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace UnityBLE.iOS
{
    /// <summary>
    /// iOS implementation of IBleDevice for real iOS devices.
    /// </summary>
    public class iOSBleDevice : UniversalBleDevice
    {

        public iOSBleDevice(string name, string address)
        {
            Name = name;
            Address = address;
            Rssi = -50;
            IsConnectable = true;
            TxPower = 4;
            AdvertisingData = "iOS advertising data";
        }

        public iOSBleDevice(string name, string address, int rssi, bool isConnectable, int txPower, string advertisingData)
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
            return new iOSConnectDeviceCommand().ExecuteAsync(Address, cancellationToken);
        }

        internal override Task<bool> ExecuteDisconnectAsync(CancellationToken cancellationToken)
        {
            return new iOSDisconnectDeviceCommand().ExecuteAsync(this, cancellationToken);
        }

        internal override Task<IReadOnlyList<IBleService>> ExecuteGetServicesCommandAsync(CancellationToken cancellationToken = default)
        {
            return new iOSGetServicesCommand().ExecuteAsync(this, cancellationToken);
        }

        internal override void EndSubscribeCharacteristic()
        {
            iOSBleNativePlugin.OnCharacteristicValue -= OnCharacteristicValueReceived;
        }

        internal override Task AutoSubscribeToCharacteristicAsync(IBleCharacteristic characteristic, CancellationToken cancellationToken = default)
        {
            //TODO: implement auto-subscribe functionality for iOS
            return Task.CompletedTask;
        }
    }
}