using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace UnityBLE.macOS
{
    /// <summary>
    /// macOS Unity Editor implementation of IBleService for testing purposes.
    /// </summary>
    public class MacOSBleService : IBleService
    {
        private readonly string _name;
        private readonly string _uuid;
        private readonly string _deviceAddress;

        public string Name => _name;
        public string Uuid => _uuid;
        public IEnumerable<IBleCharacteristic> Characteristics { get; internal set; }

        public string DeviceAddress => _deviceAddress;

        public MacOSBleService(string name, string uuid, string deviceAddress)
        {
            _name = name;
            _uuid = uuid;
            _deviceAddress = deviceAddress;
        }

        public Task<IReadOnlyList<IBleCharacteristic>> GetCharacteristicsAsync(CancellationToken cancellationToken = default)
        {
            var getCharacteristicsCommand = new MacOSGetCharacteristicsCommand();
            var characteristics = getCharacteristicsCommand.Execute(this, cancellationToken);

            Characteristics = characteristics;

            return Task.FromResult(characteristics);
        }


        public override string ToString()
        {
            return $"MacOSBleService: {_name} ({_uuid})";
        }
    }
}