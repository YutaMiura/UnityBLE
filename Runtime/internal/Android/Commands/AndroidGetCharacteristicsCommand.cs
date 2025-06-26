using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityBLE.Android
{
    internal class AndroidGetCharacteristicsCommand
    {
        private readonly AndroidJavaClass nativePlugin;

        public AndroidGetCharacteristicsCommand()
        {
            nativePlugin = new AndroidJavaClass("unityble.BlePlugin");
        }

        public Task<IReadOnlyList<IBleCharacteristic>> ExecuteAsync(IBleService service, CancellationToken cancellationToken = default)
        {
            if (service == null)
            {
                Debug.LogError("Service cannot be null.");
                return Task.FromResult<IReadOnlyList<IBleCharacteristic>>(new List<IBleCharacteristic>());
            }

            if (string.IsNullOrEmpty(service.Uuid))
            {
                Debug.LogError("Service UUID is not set.");
                return Task.FromResult<IReadOnlyList<IBleCharacteristic>>(new List<IBleCharacteristic>());
            }

            cancellationToken.ThrowIfCancellationRequested();

            var pluginInstance = nativePlugin.CallStatic<AndroidJavaObject>("getInstance");
            var characteristicUuids = pluginInstance.Call<string[]>("getCharacteristics", service.Uuid);

            if (characteristicUuids == null || characteristicUuids.Length == 0)
            {
                Debug.LogWarning($"No characteristics found for service: {service.Uuid}");
                return Task.FromResult<IReadOnlyList<IBleCharacteristic>>(new List<IBleCharacteristic>());
            }

            var characteristics = new List<IBleCharacteristic>();
            foreach (var uuid in characteristicUuids)
            {
                characteristics.Add(new AndroidBleCharacteristic(uuid, service));
            }

            return Task.FromResult<IReadOnlyList<IBleCharacteristic>>(characteristics);
        }
    }
}