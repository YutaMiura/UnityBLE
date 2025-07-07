using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityBLE.Android;

namespace UnityBLE
{
    /// <summary>
    /// Android implementation of IBleDevice.
    /// </summary>
    public class AndroidBleDevice : UniversalBleDevice
    {
        public AndroidBleDevice(string name, string address)
        {
            Name = name;
            Address = address;
            Rssi = 0;
            IsConnectable = true;
            TxPower = 0;
            AdvertisingData = "";
        }

        public AndroidBleDevice(string name, string address, int rssi, bool isConnectable, int txPower, string advertisingData)
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
            return new AndroidConnectDeviceCommand().ExecuteAsync(Address, cancellationToken);
        }

        internal override Task<bool> ExecuteDisconnectAsync(CancellationToken cancellationToken)
        {
            return new AndroidDisconnectDeviceCommand().ExecuteAsync(this, cancellationToken);
        }

        internal override Task<IReadOnlyList<IBleService>> ExecuteGetServicesCommandAsync(CancellationToken cancellationToken = default)
        {
            return new AndroidGetServicesCommand().ExecuteAsync(this, cancellationToken);
        }

        internal override void EndSubscribeCharacteristic()
        {
            //TODO: implement a subscribe message for Android
            //AndroidBleNativePlugin.OnCharacteristicValue -= OnCharacteristicValueReceived;
        }

        internal override Task AutoSubscribeToCharacteristicAsync(IBleCharacteristic characteristic, CancellationToken cancellationToken = default)
        {
            //TODO: implement auto-subscribe functionality for Android
            return Task.CompletedTask;
        }
    }
}