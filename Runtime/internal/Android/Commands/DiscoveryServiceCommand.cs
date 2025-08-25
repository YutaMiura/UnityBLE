using System;
using System.Threading.Tasks;

namespace UnityBLE.Android
{
    public class DiscoveryServiceCommand
    {
        private readonly AndroidBleNativePlugin _plugin;
        private readonly AndroidBlePeripheral _targetDevice;

        internal DiscoveryServiceCommand(AndroidBlePeripheral targetDevice, AndroidBleNativePlugin plugin)
        {
            _plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
            _targetDevice = targetDevice ?? throw new ArgumentNullException(nameof(targetDevice));
        }

        public Task<bool> ExecuteAsync()
        {
            TaskCompletionSource<bool> tcs = new();
            _plugin.OnDiscoveryServiceResult += OnDiscoveryServiceResult;
            // Start native service discovery
            _plugin.DiscoveryServices(_targetDevice);
            return tcs.Task;

            void OnDiscoveryServiceResult(int result)
            {
                if (result == 0)
                {
                    tcs.SetResult(true);
                }
                else
                {
                    tcs.SetResult(false);
                }
            }
        }
    }
}