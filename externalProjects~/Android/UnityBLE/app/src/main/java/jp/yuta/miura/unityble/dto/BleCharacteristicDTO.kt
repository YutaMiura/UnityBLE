package jp.yuta.miura.unityble.dto

import kotlinx.serialization.Serializable
import android.bluetooth.BluetoothGattCharacteristic
import jp.yuta.miura.unityble.isSupportSubscribeOperation
import jp.yuta.miura.unityble.isSupportReadOperation
import jp.yuta.miura.unityble.isSupportWriteOperation
import jp.yuta.miura.unityble.isSupportWriteWithoutResponseOperation

@Serializable
data class BleCharacteristicDTO (
    var peripheralUUID: String,
    var serviceUUID: String,
    var uuid: String,
    var isReadable: Boolean,
    var isNotifiable: Boolean,
    var isWritable: Boolean,
    var isWritableWithoutResponse: Boolean,
) {
    override fun toString(): String {
        return "Characteristic $uuid Read:$isReadable Write:$isWritable WriteWithoutResponse:$isWritableWithoutResponse subscribe:$isNotifiable"
    }
}

fun BluetoothGattCharacteristic.toDto(deviceAddress: String): BleCharacteristicDTO {
    val dto = BleCharacteristicDTO(
        peripheralUUID = deviceAddress,
        serviceUUID = service.uuid.toString(),
        uuid = this.uuid.toString(),
        isReadable = this.isSupportReadOperation(),
        isWritable = this.isSupportWriteOperation(),
        isWritableWithoutResponse = this.isSupportWriteWithoutResponseOperation(),
        isNotifiable = this.isSupportSubscribeOperation()
    )
    return dto
}