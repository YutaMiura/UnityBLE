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
        private readonly List<IBleCharacteristic> _characteristics = new();

        public string Name => _name;
        public string Uuid => _uuid;
        public IEnumerable<IBleCharacteristic> Characteristics => _characteristics;

        public MacOSBleService(string name, string uuid)
        {
            _name = name;
            _uuid = uuid;
        }

        public async Task<IReadOnlyList<IBleCharacteristic>> GetCharacteristicsAsync(CancellationToken cancellationToken = default)
        {
            var getCharacteristicsCommand = new MacOSGetCharacteristicsCommand();
            var characteristics = await getCharacteristicsCommand.ExecuteAsync(this, cancellationToken);

            _characteristics.Clear();
            _characteristics.AddRange(characteristics);

            return characteristics;
        }

        public override string ToString()
        {
            return $"MacOSBleService: {_name} ({_uuid})";
        }
    }
}