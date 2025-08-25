package jp.yuta.miura.unityble.dto

import kotlinx.serialization.Serializable
import android.bluetooth.BluetoothGattService

@Serializable
data class BleServiceDTO (
    val peripheralUUID: String,
    val uuid: String,
    val characteristics: Array<BleCharacteristicDTO>
) {
    override fun equals(other: Any?): Boolean {
        if (this === other) return true
        if (javaClass != other?.javaClass) return false

        other as BleServiceDTO

        return peripheralUUID == other.peripheralUUID && uuid == other.uuid
    }

    override fun hashCode(): Int {
        var result = peripheralUUID.hashCode()
        result = 31 * result + uuid.hashCode()
        return result
    }

    override fun toString(): String {
        return "Service: $uuid characteristics -> [${characteristics.joinToString { it.toString() }}]"
    }
}

fun BluetoothGattService.toDto(deviceAddress: String) : BleServiceDTO {
    val charDTOs = this.characteristics.map {
        it.toDto(deviceAddress)
    }.toTypedArray()
    val dto = BleServiceDTO(
        peripheralUUID = deviceAddress,
        uuid = this.uuid.toString(),
        characteristics = charDTOs
    )

    return dto
}
