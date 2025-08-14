package com.yuta.miura.unityble.dto

import android.annotation.SuppressLint
import kotlinx.serialization.Serializable
import android.bluetooth.BluetoothGatt
import android.bluetooth.BluetoothDevice

@Serializable
data class BleDeviceDTO (
    val name: String,
    val uuid: String,
    val rssi: Int,
    val services: Array<BleServiceDTO>
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
}

@SuppressLint("MissingPermission")
fun BluetoothGatt.toDto(rssi: Int) :  BleDeviceDTO {
    return BleDeviceDTO(
        name = this.device.name,
        uuid = this.device.address,
        rssi = rssi,
        services = this.services.map { it.toDto(this.device.address) }.toTypedArray()
    )
}

@SuppressLint("MissingPermission")
fun BluetoothDevice.toDto(rssi: Int): BleDeviceDTO {
    return BleDeviceDTO(
        name = this.name,
        uuid = this.address,
        rssi = rssi,
        services = emptyArray()
    )
}
