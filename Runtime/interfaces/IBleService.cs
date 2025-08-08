using System;
using System.Collections.Generic;

namespace UnityBLE
{
    /// <summary>
    /// Interface representing a BLE service.
    /// </summary>
    public interface IBleService : IDisposable
    {
        string PeripheralUUID { get; }
        string Uuid { get; }

        IEnumerable<IBleCharacteristic> Characteristics { get; }

        public delegate void CharacteristicDiscoveredDelegate(IBleCharacteristic characteristic);
        public event CharacteristicDiscoveredDelegate OnCharacteristicDiscovered;
    }
}