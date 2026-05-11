package jp.yuta.miura.unityble.dto

import android.annotation.SuppressLint
import android.bluetooth.BluetoothDevice
import android.bluetooth.BluetoothGatt
import android.bluetooth.le.ScanRecord
import android.util.Base64
import java.io.ByteArrayOutputStream
import kotlinx.serialization.Serializable

@Serializable
data class BleDeviceDTO (
    val name: String,
    val uuid: String,
    val rssi: Int,
    val services: Array<BleServiceDTO>,
    // Base64 of [companyId LE 2 bytes][payload] concatenated for every MSD
    // record. Null when the advertisement has no manufacturer data.
    val manufacturerData: String? = null,
    // service UUID (uppercase) -> Base64 of the service-data payload. Null
    // when the advertisement has no service data.
    val serviceData: Map<String, String>? = null
) {
    override fun equals(other: Any?): Boolean {
        if (this === other) return true
        if (javaClass != other?.javaClass) return false

        other as BleDeviceDTO

        return uuid == other.uuid
    }

    override fun hashCode(): Int {
        return uuid.hashCode()
    }

    override fun toString(): String {
        return "Device $name $uuid $rssi services->[${services.joinToString{ it.toString() }}]"
    }
}

@SuppressLint("MissingPermission")
fun BluetoothGatt.toDto(rssi: Int) : BleDeviceDTO {
    return BleDeviceDTO(
        name = this.device.name ?: "UNKNOWN",
        uuid = this.device.address,
        rssi = rssi,
        services = this.services.map { it.toDto(this.device.address) }.toTypedArray()
    )
}

@SuppressLint("MissingPermission")
fun BluetoothDevice.toDto(rssi: Int, scanRecord: ScanRecord? = null): BleDeviceDTO {
    return BleDeviceDTO(
        name = this.name ?: "UNKNOWN",
        uuid = this.address,
        rssi = rssi,
        services = emptyArray(),
        manufacturerData = scanRecord?.toManufacturerDataBase64(),
        serviceData = scanRecord?.toServiceDataMap()
    )
}

private fun ScanRecord.toManufacturerDataBase64(): String? {
    val msd = this.manufacturerSpecificData ?: return null
    if (msd.size() == 0) return null
    val out = ByteArrayOutputStream()
    for (i in 0 until msd.size()) {
        val companyId = msd.keyAt(i)
        val payload = msd.valueAt(i) ?: continue
        out.write(companyId and 0xFF)
        out.write((companyId shr 8) and 0xFF)
        out.write(payload)
    }
    val bytes = out.toByteArray()
    if (bytes.isEmpty()) return null
    return Base64.encodeToString(bytes, Base64.NO_WRAP)
}

private fun ScanRecord.toServiceDataMap(): Map<String, String>? {
    val sd = this.serviceData ?: return null
    if (sd.isEmpty()) return null
    val result = mutableMapOf<String, String>()
    for ((uuid, data) in sd) {
        result[uuid.toString().uppercase()] = Base64.encodeToString(data, Base64.NO_WRAP)
    }
    return if (result.isEmpty()) null else result
}
