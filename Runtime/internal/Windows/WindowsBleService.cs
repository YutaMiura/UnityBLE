using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityBLE.windows
{
    /// <summary>
    /// Windows (C++/WinRT) implementation of <see cref="IBleService"/>.
    /// </summary>
    public class WindowsBleService : IBleService
    {
        private readonly string _peripheralUUID;
        private readonly string _uuid;

        public string Uuid => _uuid;
        public string PeripheralUUID => _peripheralUUID;
        public IEnumerable<IBleCharacteristic> Characteristics => _characteristics;

        private readonly List<IBleCharacteristic> _characteristics = new();

        public event IBleService.CharacteristicDiscoveredDelegate OnCharacteristicDiscovered;

        public WindowsBleService(string uuid, string peripheralUUID, IBleCharacteristic[] characteristics = null)
        {
            _peripheralUUID = peripheralUUID;
            _uuid = uuid;
            if (characteristics != null)
            {
                _characteristics = new List<IBleCharacteristic>(characteristics);
            }

            BleDeviceEvents.OnCharacteristicDiscovered += OnCharacteristicDiscoveredHandler;
        }

        private void OnCharacteristicDiscoveredHandler(IBleCharacteristic characteristic)
        {
            Debug.Log($"OnCharacteristicDiscoveredHandler for {characteristic.Uuid} in service {_uuid}");
            if (characteristic.serviceUUID != Uuid)
            {
                Debug.LogWarning($"[UnityBLE] Characteristic {characteristic.Uuid} does not belong to service {Uuid}, skipping.");
                return;
            }
            _characteristics.Add(characteristic);
            OnCharacteristicDiscovered?.Invoke(characteristic);
        }

        internal static WindowsBleService FromDTO(ServiceDTO dto)
        {
            return new WindowsBleService(
                dto.uuid,
                dto.peripheralUUID,
                dto.characteristics?.Select(c => (IBleCharacteristic)WindowsBleCharacteristic.FromDTO(c)).ToArray() ?? Array.Empty<IBleCharacteristic>());
        }

        public override string ToString()
        {
            return $"WindowsBleService: ({_uuid})";
        }

        public void Dispose()
        {
            foreach (var c in _characteristics)
            {
                c.Dispose();
            }
            BleDeviceEvents.OnCharacteristicDiscovered -= OnCharacteristicDiscoveredHandler;
        }
    }
}
