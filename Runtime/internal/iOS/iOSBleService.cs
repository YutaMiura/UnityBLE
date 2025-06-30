using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace UnityBLE.iOS
{
    /// <summary>
    /// iOS implementation of IBleService for real iOS devices.
    /// </summary>
    public class iOSBleService : IBleService
    {
        private readonly string _name;
        private readonly string _uuid;
        private readonly List<IBleCharacteristic> _characteristics = new();
        private readonly string _deviceAddress;

        public string Name => _name;
        public string Uuid => _uuid;
        public string DeviceAddress => _deviceAddress;
        public IEnumerable<IBleCharacteristic> Characteristics => _characteristics;

        public iOSBleService(string name, string uuid)
        {
            _name = name;
            _uuid = uuid;
            _deviceAddress = "";
        }

        public iOSBleService(string name, string uuid, string deviceAddress)
        {
            _name = name;
            _uuid = uuid;
            _deviceAddress = deviceAddress;
        }

        public async Task<IReadOnlyList<IBleCharacteristic>> GetCharacteristicsAsync(CancellationToken cancellationToken = default)
        {
            var getCharacteristicsCommand = new iOSGetCharacteristicsCommand();
            var characteristics = await getCharacteristicsCommand.ExecuteAsync(this, _deviceAddress, cancellationToken);

            _characteristics.Clear();
            _characteristics.AddRange(characteristics);

            return characteristics;
        }

        public override string ToString()
        {
            return $"iOSBleService: {_name} ({_uuid})";
        }
    }
}