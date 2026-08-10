package jp.yuta.miura.unityble.dto

import kotlinx.serialization.Serializable

@Serializable
data class ReadResponse (
    val from: String,
    val value: String,
    val status: Int
)

@Serializable
data class WriteResponse (
    val from: String,
    val status: Int
)

@Serializable
data class SubscribeResponse (
    val from: String,
    val value: String,
    val status: Int
)

/**
 * Completion of a descriptor write. Enabling notifications is an asynchronous
 * GATT operation (a write to the CCCD), and until it completes the GATT stack
 * rejects any other operation on the connection as busy. Reporting it lets the
 * managed side await the subscription instead of guessing when it is safe to
 * send the first command.
 *
 * [from] is the owning characteristic's UUID; [status] is the raw
 * BluetoothGatt status (0 = GATT_SUCCESS).
 */
@Serializable
data class DescriptorWriteResponse (
    val from: String,
    val descriptor: String,
    val status: Int
)