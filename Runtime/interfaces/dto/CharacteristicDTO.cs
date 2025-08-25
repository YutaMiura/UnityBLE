using System;

namespace UnityBLE
{
    [Serializable]
    internal class CharacteristicDTO
    {
        public string peripheralUUID;
        public string serviceUUID;
        public string uuid;
        public string value;

        public bool isReadable;
        public bool isWritable;
        public bool isNotifiable;
    }
}