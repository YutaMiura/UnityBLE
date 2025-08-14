package com.yuta.miura.unityble.dto

import kotlinx.serialization.Serializable
import android.bluetooth.BluetoothGattCharacteristic
import com.yuta.miura.unityble.isSupportSubscribeOperation
import com.yuta.miura.unityble.isSupportReadOperation
import com.yuta.miura.unityble.isSupportWriteOperation

@Serializable
data class BleCharacteristicDTO (
    var peripheralUUID: String,
    var serviceUUID: String,
    var uuid: String,
    var isReadable: Boolean,
    var isNotifiable: Boolean,
    var isWritable: Boolean,
)

fun BluetoothGattCharacteristic.toDto(deviceAddress: String): BleCharacteristicDTO {
    val dto = BleCharacteristicDTO(
        peripheralUUID = deviceAddress,
        serviceUUID = service.uuid.toString(),
        uuid = this.uuid.toString(),
        isReadable = this.isSupportReadOperation(),
        isWritable = this.isSupportWriteOperation(),
        isNotifiable = this.isSupportSubscribeOperation()
    )
    return dto
}