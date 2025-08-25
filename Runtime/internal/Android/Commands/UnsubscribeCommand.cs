using System.Threading.Tasks;

namespace UnityBLE.Android
{
    public class UnsubscribeCommand
    {
        private readonly string _characteristicUuid;
        private readonly string _serviceUuid;
        private readonly string _peripheralUuid;
        private readonly AndroidBleNativePlugin _plugin;

        internal UnsubscribeCommand(
            string characteristicUuid,
            string serviceUuid,
            string peripheralUuid,
            AndroidBleNativePlugin plugin)
        {
            _characteristicUuid = characteristicUuid;
            _serviceUuid = serviceUuid;
            _peripheralUuid = peripheralUuid;
            _plugin = plugin;
        }

        public Task ExecuteAsync()
        {
            return _plugin.UnsubscribeAsync(_characteristicUuid, _serviceUuid, _peripheralUuid);
        }
    }
}