using System;

namespace UnityBLE
{
    /// <summary>
    /// BLE characteristic properties as defined in the Bluetooth specification.
    /// These flags indicate which operations are supported by a characteristic.
    /// </summary>
    [Flags]
    public enum CharacteristicProperties
    {
        /// <summary>
        /// No properties supported
        /// </summary>
        None = 0,

        /// <summary>
        /// Characteristic supports broadcasting
        /// </summary>
        Broadcast = 0x01,

        /// <summary>
        /// Characteristic supports reading
        /// </summary>
        Read = 0x02,

        /// <summary>
        /// Characteristic supports write without response
        /// </summary>
        WriteWithoutResponse = 0x04,

        /// <summary>
        /// Characteristic supports write with response
        /// </summary>
        Write = 0x08,

        /// <summary>
        /// Characteristic supports notifications
        /// </summary>
        Notify = 0x10,

        /// <summary>
        /// Characteristic supports indications
        /// </summary>
        Indicate = 0x20,

        /// <summary>
        /// Characteristic supports authenticated signed writes
        /// </summary>
        AuthenticatedSignedWrites = 0x40,

        /// <summary>
        /// Characteristic has extended properties
        /// </summary>
        ExtendedProperties = 0x80
    }

    /// <summary>
    /// Extension methods for CharacteristicProperties
    /// </summary>
    public static class CharacteristicPropertiesExtensions
    {
        /// <summary>
        /// Check if the characteristic supports reading
        /// </summary>
        public static bool CanRead(this CharacteristicProperties properties)
        {
            return properties.HasFlag(CharacteristicProperties.Read);
        }

        /// <summary>
        /// Check if the characteristic supports writing (with or without response)
        /// </summary>
        public static bool CanWrite(this CharacteristicProperties properties)
        {
            return properties.HasFlag(CharacteristicProperties.Write) ||
                   properties.HasFlag(CharacteristicProperties.WriteWithoutResponse);
        }

        /// <summary>
        /// Check if the characteristic supports notifications or indications
        /// </summary>
        public static bool CanNotify(this CharacteristicProperties properties)
        {
            return properties.HasFlag(CharacteristicProperties.Notify) ||
                   properties.HasFlag(CharacteristicProperties.Indicate);
        }

        /// <summary>
        /// Check if the characteristic supports write with response
        /// </summary>
        public static bool CanWriteWithResponse(this CharacteristicProperties properties)
        {
            return properties.HasFlag(CharacteristicProperties.Write);
        }

        /// <summary>
        /// Check if the characteristic supports write without response
        /// </summary>
        public static bool CanWriteWithoutResponse(this CharacteristicProperties properties)
        {
            return properties.HasFlag(CharacteristicProperties.WriteWithoutResponse);
        }
    }
}