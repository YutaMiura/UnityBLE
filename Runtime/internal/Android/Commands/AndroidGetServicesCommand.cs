using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityBLE.Android
{
    internal class AndroidGetServicesCommand
    {
        private readonly AndroidJavaClass nativePlugin;

        public AndroidGetServicesCommand()
        {
            nativePlugin = new AndroidJavaClass("unityble.BlePlugin");
        }

        public Task<IReadOnlyList<IBleService>> ExecuteAsync(IBleDevice device, CancellationToken cancellationToken = default)
        {
            if (device == null)
            {
                Debug.LogError("Device cannot be null.");
                return Task.FromResult<IReadOnlyList<IBleService>>(new List<IBleService>());
            }

            if (string.IsNullOrEmpty(device.Address))
            {
                Debug.LogError("Device address is not set.");
                return Task.FromResult<IReadOnlyList<IBleService>>(new List<IBleService>());
            }

            if (!device.IsConnected)
            {
                Debug.LogError($"Device {device.Address} is not connected.");
                return Task.FromResult<IReadOnlyList<IBleService>>(new List<IBleService>());
            }

            cancellationToken.ThrowIfCancellationRequested();

            var pluginInstance = nativePlugin.CallStatic<AndroidJavaObject>("getInstance");
            var serviceUuids = pluginInstance.Call<string[]>("getServices", device.Address);

            if (serviceUuids == null || serviceUuids.Length == 0)
            {
                Debug.LogWarning($"No services found for device: {device.Address}");
                return Task.FromResult<IReadOnlyList<IBleService>>(new List<IBleService>());
            }

            var services = new List<IBleService>();
            foreach (var uuid in serviceUuids)
            {
                services.Add(new AndroidBleService(uuid, device.Address));
            }

            return Task.FromResult<IReadOnlyList<IBleService>>(services);
        }
    }
}