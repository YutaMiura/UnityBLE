using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityBLE.Android;

namespace UnityBLE
{
    /// <summary>
    /// Android implementation of IBleService.
    /// </summary>
    public class AndroidBleService : IBleService
    {
        private readonly string _uuid;
        private readonly string _deviceAddress;
        private List<IBleCharacteristic> _characteristics = new();

        public AndroidBleService(string uuid, string deviceAddress)
        {
            _uuid = uuid;
            _deviceAddress = deviceAddress;
        }

        public string Uuid => _uuid;

        public IEnumerable<IBleCharacteristic> Characteristics => _characteristics;

        public string DeviceAddress => _deviceAddress;

        public async Task<IReadOnlyList<IBleCharacteristic>> GetCharacteristicsAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Debug.Log($"[Android BLE] Discovering characteristics for service {_uuid}...");

            var command = new AndroidGetCharacteristicsCommand();
            try
            {
                var characteristics = await command.ExecuteAsync(this, cancellationToken);
                
                // Update internal cache
                _characteristics.Clear();
                _characteristics.AddRange(characteristics);
                
                Debug.Log($"[Android BLE] Found {characteristics.Count} characteristics for service {_uuid}");
                return characteristics;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Android BLE] Error discovering characteristics for service {_uuid}: {ex.Message}");
                throw;
            }
            finally
            {
                command.Dispose();
            }
        }

        public override string ToString()
        {
            return $"AndroidBleService: {_uuid}";
        }
    }
}