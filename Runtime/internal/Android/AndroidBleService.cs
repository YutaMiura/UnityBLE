using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityBLE.Android
{
    public class AndroidBleService : IBleService
    {
        private readonly string _peripheralUUID;
        private readonly string _uuid;

        public string Uuid => _uuid;
        public string PeripheralUUID => _peripheralUUID;

        private ConcurrentDictionary<string, IBleCharacteristic> _characteristics = new();

        public IEnumerable<IBleCharacteristic> Characteristics => _characteristics.Values;

        public event IBleService.CharacteristicDiscoveredDelegate OnCharacteristicDiscovered;

        public AndroidBleService(string uuid, string peripheralUUID, IBleCharacteristic[] characteristics = null)
        {
            _peripheralUUID = peripheralUUID;
            _uuid = uuid;
            if (characteristics != null)
            {
                _characteristics = new ConcurrentDictionary<string, IBleCharacteristic>(characteristics.ToDictionary(c => c.Uuid));
            }

            BleDeviceEvents.OnCharacteristicDiscovered += OnCharacteristicDiscoveredHandler;
        }

        private void OnCharacteristicDiscoveredHandler(IBleCharacteristic characteristic)
        {
            Debug.Log($"OnCharacteristicDiscoveredHandler at {characteristic.Uuid} in service {characteristic.serviceUUID}");
            if (characteristic.serviceUUID != Uuid)
            {
                Debug.LogWarning($"[UnityBLE] Characteristic {characteristic.Uuid} does not belong to service {Uuid}, skipping.");
                return;
            }
            if (_characteristics.TryAdd(characteristic.Uuid, characteristic))
            {
                OnCharacteristicDiscovered?.Invoke(characteristic);
            }
        }

        internal static AndroidBleService FromDTO(ServiceDTO dto)
        {
            var service = new AndroidBleService(dto.uuid, dto.peripheralUUID, dto.characteristics?.Select(c => AndroidBleCharacteristic.FromDTO(c)).ToArray() ?? Array.Empty<IBleCharacteristic>());
            foreach (var characteristic in dto.characteristics)
            {
                if (characteristic == null)
                {
                    Debug.LogWarning($"Characteristic in DTO is null for service {dto.uuid}");
                    continue;
                }

                var chara = AndroidBleCharacteristic.FromDTO(characteristic);
                service._characteristics.TryAdd(chara.Uuid, chara);
            }
            return service;
        }


        public override string ToString()
        {
            return $"AndroidBleService: ({_uuid}) characteristics: [{string.Join(", ", _characteristics.Values.Select(c => c.Uuid))}]";
        }

        public void Dispose()
        {
            foreach (var c in _characteristics.Values)
            {
                c.Dispose();
            }
            BleDeviceEvents.OnCharacteristicDiscovered -= OnCharacteristicDiscoveredHandler;
        }
    }
}