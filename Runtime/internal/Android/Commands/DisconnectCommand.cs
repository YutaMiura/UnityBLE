using System;
using System.Threading;
using System.Threading.Tasks;

namespace UnityBLE.Android
{
    public class DisconnectCommand
    {
        private readonly AndroidBleNativePlugin _plugin;
        private readonly AndroidBlePeripheral _targetDevice;
        private const int DISCONNECT_TIMEOUT_MS = 10000; // 10 seconds timeout

        internal DisconnectCommand(AndroidBlePeripheral targetDevice, AndroidBleNativePlugin plugin)
        {
            _plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
            _targetDevice = targetDevice ?? throw new ArgumentNullException(nameof(targetDevice));
        }

        public async Task<bool> ExecuteAsync()
        {
            var t = new TaskCompletionSource<bool>();
            BleDeviceEvents.OnDisconnected += OnDisconnected;

            try
            {
                _plugin.Disconnect(_targetDevice);

                // Wait for disconnection event with timeout
                using (var cts = new CancellationTokenSource(DISCONNECT_TIMEOUT_MS))
                {
                    var timeoutTask = Task.Delay(DISCONNECT_TIMEOUT_MS, cts.Token);
                    var completedTask = await Task.WhenAny(t.Task, timeoutTask);

                    if (completedTask == timeoutTask)
                    {
                        throw new TimeoutException($"Disconnect operation timed out after {DISCONNECT_TIMEOUT_MS}ms for device {_targetDevice.UUID}");
                    }

                    cts.Cancel(); // Cancel the timeout task if disconnection succeeded
                    return await t.Task;
                }
            }
            finally
            {
                BleDeviceEvents.OnDisconnected -= OnDisconnected;
            }

            void OnDisconnected(string deviceUuid)
            {
                if (deviceUuid == _targetDevice.UUID)
                {
                    t.TrySetResult(true);
                }
            }
        }
    }
}