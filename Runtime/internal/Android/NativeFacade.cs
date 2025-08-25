using System;
using System.Threading;
using System.Threading.Tasks;

namespace UnityBLE.Android
{
    internal sealed class NativeFacade
    {
        private readonly AndroidBleNativePlugin Plugin;

        private static NativeFacade _instance;
        public static NativeFacade Instance => _instance ??= new NativeFacade();

        private BleScanEventDelegates.DeviceDiscoveredDelegate _onDeviceDiscovered;

        private NativeFacade()
        {
            Plugin = new AndroidBleNativePlugin();
        }

        internal async Task StartScan(ScanFilter filter, BleScanEventDelegates.DeviceDiscoveredDelegate OnDeviceDiscovered)
        {
            if (_onDeviceDiscovered != null)
            {
                BleScanEventDelegates.OnDeviceDiscovered -= _onDeviceDiscovered;
            }
            _onDeviceDiscovered = OnDeviceDiscovered;
            BleScanEventDelegates.OnDeviceDiscovered += _onDeviceDiscovered;
            try
            {
                if (filter == null)
                {
                    filter = ScanFilter.None;
                }
                await Plugin.StartScanAsync(filter);
            }
            catch
            {
                BleScanEventDelegates.OnDeviceDiscovered -= OnDeviceDiscovered;
                throw;
            }
        }

        internal async Task<bool> StopScanAsync()
        {
            try
            {
                await Plugin.StopScanAsync();
                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                if (_onDeviceDiscovered != null)
                {
                    BleScanEventDelegates.OnDeviceDiscovered -= _onDeviceDiscovered;
                    _onDeviceDiscovered = null;
                }
            }
        }

        public Task<IBlePeripheral> ConnectAsync(AndroidBlePeripheral targetDevice)
        {
            var command = new ConnectCommand(targetDevice, Plugin);
            return command.ExecuteAsync();
        }

        public Task<bool> DiscoverServices(AndroidBlePeripheral peripheral)
        {
            var command = new DiscoveryServiceCommand(peripheral, Plugin);
            return command.ExecuteAsync();
        }

        public Task<bool> DisconnectAsync(AndroidBlePeripheral peripheral)
        {
            var command = new DisconnectCommand(peripheral, Plugin);
            return command.ExecuteAsync();
        }

        public Task<byte[]> ReadAsync(AndroidBleCharacteristic characteristic)
        {
            var command = new ReadCharacteristicCommand(characteristic, Plugin);
            return command.ExecuteAsync();
        }

        public Task WriteAsync(AndroidBleCharacteristic characteristic, byte[] data)
        {
            var command = new WriteCommand(characteristic, Plugin);
            return command.ExecuteAsync(data);
        }

        public SubscribeCommand CreateSubscribeCommand(AndroidBleCharacteristic characteristic, IBleCharacteristic.DataReceivedDelegate onDataReceived)
        {
            return new SubscribeCommand(characteristic.Uuid, characteristic.serviceUUID, characteristic.peripheralUUID, Plugin, onDataReceived);
        }

        public Task UnsubscribeAsync(AndroidBleCharacteristic characteristic)
        {
            var command = new UnsubscribeCommand(characteristic.peripheralUUID, characteristic.serviceUUID, characteristic.Uuid, Plugin);
            return command.ExecuteAsync();
        }
    }
}