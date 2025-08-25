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
            // Start native disconnection
            _plugin.Disconnect(_targetDevice);
            return Task.FromResult(true);
        }
    }
}