using System;
using System.Threading.Tasks;

namespace UnityBLE.Android
{
    public class DisconnectCommand
    {
        private readonly AndroidBleNativePlugin _plugin;
        private readonly AndroidBlePeripheral _targetDevice;
        internal DisconnectCommand(AndroidBlePeripheral targetDevice, AndroidBleNativePlugin plugin)
        {
            _plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
            _targetDevice = targetDevice ?? throw new ArgumentNullException(nameof(targetDevice));
        }

        public Task<bool> ExecuteAsync()
        {
            var t = new TaskCompletionSource<bool>();
            BleDeviceEvents.OnDisconnected += OnDisconnected;
            _plugin.Disconnect(_targetDevice);
            return t.Task;

            void OnDisconnected(string deviceUuid)
            {
                if (deviceUuid == _targetDevice.UUID)
                {
                    BleDeviceEvents.OnDisconnected -= OnDisconnected;
                    t.SetResult(true);
                }
            }
        }
    }
}