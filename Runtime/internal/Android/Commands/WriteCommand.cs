using System;
using System.Threading.Tasks;

namespace UnityBLE.Android
{
    public class WriteCommand
    {
        private readonly AndroidBleNativePlugin _plugin;
        private readonly AndroidBleCharacteristic _characteristic;

        internal WriteCommand(AndroidBleCharacteristic characteristic, AndroidBleNativePlugin plugin)
        {
            _plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
            _characteristic = characteristic ?? throw new ArgumentNullException(nameof(characteristic));
        }

        public Task ExecuteAsync(byte[] data)
        {
            return _plugin.WriteAsync(_characteristic, data);
        }
    }
}