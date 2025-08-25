using System;

namespace UnityBLE
{
    [Serializable]
    internal class ServiceDTO
    {
        public string peripheralUUID;
        public string uuid;
        public string description;
        public CharacteristicDTO[] characteristics;
    }
}