using System;
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

        private List<IBleCharacteristic> _characteristics = new();

        public IEnumerable<IBleCharacteristic> Characteristics => _characteristics;

        public event IBleService.CharacteristicDiscoveredDelegate OnCharacteristicDiscovered;

        public AndroidBleService(string uuid, string peripheralUUID, IBleCharacteristic[] characteristics = null)
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
            if (characteristic.CanNotify)
            {
                Debug.Log($"Subscribing to notifications for characteristic {characteristic.Uuid}");
                characteristic.Subscribe();
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

                service._characteristics.Add(AndroidBleCharacteristic.FromDTO(characteristic));
            }
            return service;
        }


        public override string ToString()
        {
            return $"AndroidBleService: ({_uuid})";
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