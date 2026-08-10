using System;

namespace UnityBLE.Android
{
    /// <summary>
    /// Completion of a descriptor write, forwarded from the native
    /// onDescriptorWrite callback. Enabling notifications is an asynchronous GATT
    /// operation (a write to the CCCD) during which the stack rejects every other
    /// operation on the connection, so this is what tells us a subscription has
    /// actually taken effect and the first command is safe to send.
    /// </summary>
    [Serializable]
    public class DescriptorWriteResponseDTO
    {
        /// UUID of the characteristic that owns the descriptor.
        public string from;

        /// UUID of the descriptor that was written (the CCCD, for subscriptions).
        public string descriptor;

        /// Raw BluetoothGatt status: 0 is success. Negative means the write could
        /// not be issued at all, so no native callback is coming.
        public int status;
    }
}
