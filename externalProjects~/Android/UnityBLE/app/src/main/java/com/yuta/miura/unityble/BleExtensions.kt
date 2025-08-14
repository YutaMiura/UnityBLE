package com.yuta.miura.unityble

import android.bluetooth.BluetoothGattCharacteristic
import android.util.Base64


fun BluetoothGattCharacteristic.isSupportReadOperation(): Boolean {
    return this.properties and BluetoothGattCharacteristic.PROPERTY_READ != 0
}

fun BluetoothGattCharacteristic.isSupportWriteOperation(): Boolean {
    return this.properties and (
            BluetoothGattCharacteristic.PROPERTY_WRITE or BluetoothGattCharacteristic.PROPERTY_SIGNED_WRITE
    ) != 0
}

fun BluetoothGattCharacteristic.isSupportSubscribeOperation():Boolean {
    return this.properties and (
            BluetoothGattCharacteristic.PROPERTY_NOTIFY or BluetoothGattCharacteristic.PROPERTY_INDICATE
    ) != 0
}


fun ByteArray.toBase64String(): String {
    return Base64.encodeToString(this, Base64.NO_WRAP)
}