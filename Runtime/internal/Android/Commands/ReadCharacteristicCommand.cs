using System;
using System.Threading.Tasks;

namespace UnityBLE.Android
{
    public class ReadCharacteristicCommand
    {
        private readonly AndroidBleNativePlugin _plugin;
        private readonly AndroidBleCharacteristic _characteristic;

        internal ReadCharacteristicCommand(AndroidBleCharacteristic characteristic, AndroidBleNativePlugin plugin)
        {
            _plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
            _characteristic = characteristic ?? throw new ArgumentNullException(nameof(characteristic));
        }

        public async Task<string> ExecuteAsync()
        {
            return await _plugin.ReadAsync(_characteristic);
        }
    }
}