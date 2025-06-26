using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityBLE.Android
{
    public class AndroidDisconnectDeviceCommand
    {
        private readonly AndroidJavaClass nativePlugin;

        public AndroidDisconnectDeviceCommand()
        {
            nativePlugin = new AndroidJavaClass("unityble.BlePlugin");
        }

        public Task<bool> ExecuteAsync(IBleDevice device, CancellationToken cancellationToken = default)
        {
            if (device == null)
            {
                Debug.LogError("Device cannot be null.");
                return Task.FromResult(false);
            }

            if (string.IsNullOrEmpty(device.Address))
            {
                Debug.LogError("Device address is not set.");
                return Task.FromResult(false);
            }

            if (!device.IsConnected)
            {
                Debug.LogWarning($"Device {device.Address} is not connected.");
                return Task.FromResult(false);
            }

            cancellationToken.ThrowIfCancellationRequested();

            var pluginInstance = nativePlugin.CallStatic<AndroidJavaObject>("getInstance");
            if (pluginInstance.Call<bool>("disconnectDevice", device.Address))
            {
                return Task.FromResult(true);
            }
            else
            {
                Debug.LogError($"Failed to disconnect device: {device.Address}");
                return Task.FromResult(false);
            }
        }
    }
}