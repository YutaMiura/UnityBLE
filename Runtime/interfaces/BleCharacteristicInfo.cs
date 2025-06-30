namespace UnityBLE
{
    /// <summary>
    /// Contains information about a BLE characteristic including its properties.
    /// </summary>
    public class BleCharacteristicInfo
    {
        public string Uuid { get; }
        public string Name { get; }
        public CharacteristicProperties Properties { get; }

        public BleCharacteristicInfo(string uuid, string name, CharacteristicProperties properties)
        {
            Uuid = uuid;
            Name = name;
            Properties = properties;
        }

        /// <summary>
        /// Check if the characteristic supports reading
        /// </summary>
        public bool CanRead => Properties.CanRead();

        /// <summary>
        /// Check if the characteristic supports writing
        /// </summary>
        public bool CanWrite => Properties.CanWrite();

        /// <summary>
        /// Check if the characteristic supports notifications
        /// </summary>
        public bool CanNotify => Properties.CanNotify();

        public override string ToString()
        {
            return $"BleCharacteristicInfo: {Name} ({Uuid}) Properties: {Properties}";
        }
    }
}