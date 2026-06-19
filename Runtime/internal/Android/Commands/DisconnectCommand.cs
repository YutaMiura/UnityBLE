using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

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
            Debug.LogWarning($"[GranBoardDisconnect] UnityBLE DisconnectCommand.ExecuteAsync called. uuid={_targetDevice.UUID}");
            var t = new TaskCompletionSource<bool>();
            BleDeviceEvents.OnDisconnected += OnDisconnected;

            try
            {
                Debug.LogWarning($"[GranBoardDisconnect] UnityBLE DisconnectCommand calling plugin Disconnect. uuid={_targetDevice.UUID}");
                _plugin.Disconnect(_targetDevice);
                Debug.LogWarning($"[GranBoardDisconnect] UnityBLE DisconnectCommand plugin Disconnect returned. waiting OnDisconnected. uuid={_targetDevice.UUID}");

                // Wait for disconnection event with timeout
                using (var cts = new CancellationTokenSource(DISCONNECT_TIMEOUT_MS))
                {
                    var timeoutTask = Task.Delay(DISCONNECT_TIMEOUT_MS, cts.Token);
                    var completedTask = await Task.WhenAny(t.Task, timeoutTask);

                    if (completedTask == timeoutTask)
                    {
                        Debug.LogWarning($"[GranBoardDisconnect] UnityBLE DisconnectCommand timed out waiting OnDisconnected. uuid={_targetDevice.UUID}");
                        throw new TimeoutException($"Disconnect operation timed out after {DISCONNECT_TIMEOUT_MS}ms for device {_targetDevice.UUID}");
                    }

                    cts.Cancel(); // Cancel the timeout task if disconnection succeeded
                    Debug.LogWarning($"[GranBoardDisconnect] UnityBLE DisconnectCommand OnDisconnected received. uuid={_targetDevice.UUID}");
                    return await t.Task;
                }
            }
            finally
            {
                Debug.LogWarning($"[GranBoardDisconnect] UnityBLE DisconnectCommand cleanup unsubscribe OnDisconnected. uuid={_targetDevice.UUID}");
                BleDeviceEvents.OnDisconnected -= OnDisconnected;
            }

            void OnDisconnected(string deviceUuid)
            {
                Debug.LogWarning($"[GranBoardDisconnect] UnityBLE DisconnectCommand OnDisconnected event. expected={_targetDevice.UUID}, actual={deviceUuid}");
                if (deviceUuid == _targetDevice.UUID)
                {
                    t.TrySetResult(true);
                }
            }
        }
    }
}
