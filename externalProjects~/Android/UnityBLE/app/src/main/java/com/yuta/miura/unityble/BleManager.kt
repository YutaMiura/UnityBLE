package com.yuta.miura.unityble

import android.annotation.SuppressLint
import android.app.Activity
import android.bluetooth.BluetoothAdapter
import android.bluetooth.BluetoothDevice
import android.bluetooth.BluetoothGatt
import android.bluetooth.BluetoothGattCallback
import android.bluetooth.BluetoothGattCharacteristic
import android.bluetooth.BluetoothManager
import android.bluetooth.BluetoothProfile
import android.bluetooth.le.ScanCallback
import android.bluetooth.le.ScanFilter
import android.bluetooth.le.ScanResult
import android.bluetooth.le.ScanSettings
import android.content.Context
import android.content.pm.PackageManager
import android.os.Build
import android.os.ParcelUuid
import com.unity3d.player.UnityPlayer
import com.yuta.miura.unityble.unity.UnityBleEventDispatcher
import com.yuta.miura.unityble.unity.UnityLogger
import java.util.UUID

class BleManager private constructor(private val activity: Activity) {
    private val bleManager : BluetoothManager = activity.getSystemService(Context.BLUETOOTH_SERVICE) as BluetoothManager
    private val bleAdapter : BluetoothAdapter? = bleManager.adapter
    private val permissionService: PermissionService = PermissionService(activity)
    private val unityEventDispatcher = UnityBleEventDispatcher()
    private val foundDevices: MutableMap<String, BluetoothDevice> = mutableMapOf()
    private val connectedDevices: MutableMap<String, BluetoothGatt> = mutableMapOf()

    companion object  {
        @JvmStatic
        val instance: BleManager by lazy {
            BleManager(UnityPlayer.currentActivity)
        }
    }

    @SuppressLint("MissingPermission")
    fun startBleScan(names: Array<String>, serviceUUIDs: Array<String>) {
        if(isBleSupported()) {
            unityEventDispatcher.notifyResultForInvokeScan(UnityBleEventDispatcher.ScanResult.BLE_FEATURE_NOT_FOUND)
            return
        }
        val filters = ArrayList<ScanFilter>()
        for (name in names) {
            filters.add(ScanFilter.Builder().setDeviceName(name).build())
        }
        for (service in serviceUUIDs) {
            val uuid = ParcelUuid(UUID.fromString(service))
            filters.add(ScanFilter.Builder().setServiceUuid(uuid).build())
        }
        val setting = ScanSettings.Builder().build()

        permissionService.ensurePermissionsWithResult(permissionService.requiredPermissionsForScan()
        ) { result ->
            when (result) {
                PermissionService.PermissionResult.ReadyForUse -> {
                    try {
                        bleAdapter?.bluetoothLeScanner?.startScan(filters, setting, scanCallback)
                        unityEventDispatcher.notifyResultForInvokeScan(UnityBleEventDispatcher.ScanResult.OK)
                    } catch (e: Exception) {
                        UnityLogger.e("Error on scan ${e.message}")
                        unityEventDispatcher.notifyResultForInvokeScan(UnityBleEventDispatcher.ScanResult.UNKNOWN)
                    }

                }
                PermissionService.PermissionResult.LocationServiceDisabled -> {
                    unityEventDispatcher.notifyResultForInvokeScan(UnityBleEventDispatcher.ScanResult.LOCATION_SERVICE_DISABLED)
                }
                PermissionService.PermissionResult.SomePermissionsDined -> {
                    unityEventDispatcher.notifyResultForInvokeScan(UnityBleEventDispatcher.ScanResult.PERMISSION_DENIED)
                }
            }

        }
    }

    @SuppressLint("MissingPermission")
    fun stopScan() {
        if(isBleSupported()) {
            unityEventDispatcher.notifyResultForInvokeStopScan(UnityBleEventDispatcher.ScanResult.BLE_FEATURE_NOT_FOUND)
            return
        }
        permissionService.ensurePermissionsWithResult(permissionService.requiredPermissionsForScan()
        ) {
            result ->
            when(result) {
                PermissionService.PermissionResult.ReadyForUse -> {
                    bleAdapter?.bluetoothLeScanner?.stopScan(scanCallback)
                    unityEventDispatcher.notifyResultForInvokeStopScan(UnityBleEventDispatcher.ScanResult.OK)
                }
                PermissionService.PermissionResult.LocationServiceDisabled -> {
                    unityEventDispatcher.notifyResultForInvokeStopScan(UnityBleEventDispatcher.ScanResult.LOCATION_SERVICE_DISABLED)
                }
                PermissionService.PermissionResult.SomePermissionsDined -> {
                    unityEventDispatcher.notifyResultForInvokeStopScan(UnityBleEventDispatcher.ScanResult.PERMISSION_DENIED)
                }
            }
        }

    }

    private fun isBleSupported(): Boolean {
        val pm = activity.packageManager
        return pm.hasSystemFeature(PackageManager.FEATURE_BLUETOOTH_LE) && bleAdapter != null
    }

    @SuppressLint("MissingPermission")
    fun connect(address: String) {
        val device = bleAdapter?.getRemoteDevice(address)
        if(device == null) {
            unityEventDispatcher.notifyResultForInvokeConnect(UnityBleEventDispatcher.ConnectResult.DEVICE_NOT_FOUND)
            return
        }
        if(connectedDevices.containsKey(address)) {
            unityEventDispatcher.notifyResultForInvokeConnect(UnityBleEventDispatcher.ConnectResult.ALREADY_CONNECTED)
            return
        }

        permissionService.ensurePermissionsWithResult(permissionService.requiredPermissionsForConnect()
        ) {
            result ->
            when (result) {
                PermissionService.PermissionResult.ReadyForUse -> {
                    try {
                        device.connectGatt(activity, false, gattCallback)
                    } catch (e: Exception) {
                        UnityLogger.e("Error on connect ${e.message}")
                        unityEventDispatcher.notifyResultForInvokeConnect(UnityBleEventDispatcher.ConnectResult.UNKNOWN)
                    }
                }
                PermissionService.PermissionResult.LocationServiceDisabled -> {
                    unityEventDispatcher.notifyResultForInvokeConnect(UnityBleEventDispatcher.ConnectResult.PERMISSION_DENIED)
                }
                PermissionService.PermissionResult.SomePermissionsDined -> {
                    unityEventDispatcher.notifyResultForInvokeConnect(UnityBleEventDispatcher.ConnectResult.PERMISSION_DENIED)
                }
            }
        }
    }

    @SuppressLint("MissingPermission")
    fun disconnect(address: String) {
        connectedDevices[address]?.close()
    }

    @SuppressLint("MissingPermission")
    fun discoveryServices(address: String) {
        if(!connectedDevices.containsKey(address)) {
            unityEventDispatcher.notifyResultForDiscoveryService(UnityBleEventDispatcher.DiscoverServiceResult.NOT_CONNECTED)
            return
        }
        permissionService.ensurePermissionsWithResult(permissionService.requiredPermissionsForConnect()
        ) {
            result ->
            when(result) {
                PermissionService.PermissionResult.ReadyForUse -> {
                    connectedDevices[address]?.discoverServices()
                }
                PermissionService.PermissionResult.LocationServiceDisabled -> {
                    unityEventDispatcher.notifyResultForDiscoveryService(UnityBleEventDispatcher.DiscoverServiceResult.PERMISSION_DENIED)
                }
                PermissionService.PermissionResult.SomePermissionsDined -> {
                    unityEventDispatcher.notifyResultForDiscoveryService(UnityBleEventDispatcher.DiscoverServiceResult.PERMISSION_DENIED)
                }
            }
        }
    }

    @SuppressLint("MissingPermission")
    fun read(characteristicUUID: String, serviceUUID: String, peripheralUUID: String): Int{
        val gatt = connectedDevices[peripheralUUID]
            ?: return UnityBleEventDispatcher.ReadResult.DEVICE_NOT_FOUND.ordinal

        val sUUID = try {
            UUID.fromString(serviceUUID)
        } catch (e: IllegalArgumentException) {
            UnityLogger.e("Invalid service UUID $serviceUUID")
            return UnityBleEventDispatcher.ReadResult.UNKNOWN.ordinal
        }

        val cUUID = try {
            UUID.fromString(characteristicUUID)
        } catch (e: IllegalArgumentException) {
            UnityLogger.e("Invalid characteristic UUID $characteristicUUID")
            return UnityBleEventDispatcher.ReadResult.UNKNOWN.ordinal
        }

        val char = gatt.getService(sUUID).getCharacteristic(cUUID)
        if(!char.isSupportReadOperation()) {
            return UnityBleEventDispatcher.ReadResult.OPERATION_NOT_SUPPORTED.ordinal
        }

        permissionService.ensurePermissionsWithResult(permissionService.requiredPermissionsForConnect()
        ) {
            result ->
            when(result) {
                PermissionService.PermissionResult.ReadyForUse -> gatt.readCharacteristic(char)
                PermissionService.PermissionResult.LocationServiceDisabled -> unityEventDispatcher.notifyOnRead(char, "", UnityBleEventDispatcher.ReadResult.PERMISSION_DENIED)
                PermissionService.PermissionResult.SomePermissionsDined -> unityEventDispatcher.notifyOnRead(char, "", UnityBleEventDispatcher.ReadResult.PERMISSION_DENIED)
            }
        }

        return UnityBleEventDispatcher.ReadResult.OK.ordinal
    }

    @SuppressLint("MissingPermission")
    fun write(characteristicUUID: String, serviceUUID: String, peripheralUUID: String, value: ByteArray): Int{
        val gatt = connectedDevices[peripheralUUID]
            ?: return UnityBleEventDispatcher.WriteResult.DEVICE_NOT_FOUND.ordinal

        val sUUID = try {
            UUID.fromString(serviceUUID)
        } catch (e: IllegalArgumentException) {
            UnityLogger.e("Invalid service UUID $serviceUUID")
            return UnityBleEventDispatcher.WriteResult.UNKNOWN.ordinal
        }

        val cUUID = try {
            UUID.fromString(characteristicUUID)
        } catch (e: IllegalArgumentException) {
            UnityLogger.e("Invalid characteristic UUID $characteristicUUID")
            return UnityBleEventDispatcher.WriteResult.UNKNOWN.ordinal
        }

        val char = gatt.getService(sUUID).getCharacteristic(cUUID)
        if(!char.isSupportWriteOperation()) {
            return UnityBleEventDispatcher.WriteResult.OPERATION_NOT_SUPPORTED.ordinal
        }

        permissionService.ensurePermissionsWithResult(permissionService.requiredPermissionsForConnect()
        ) {
            result ->
            when(result) {
                PermissionService.PermissionResult.ReadyForUse -> {
                    if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
                        try {
                            gatt.writeCharacteristic(char, value, BluetoothGattCharacteristic.WRITE_TYPE_DEFAULT)
                            unityEventDispatcher.notifyOnWrite(from = characteristicUUID, result = UnityBleEventDispatcher.WriteResult.OK)
                        } catch(e: IllegalArgumentException) {
                            UnityLogger.e("Write failed. characteristic may be null.")
                            unityEventDispatcher.notifyOnWrite(from = characteristicUUID, result = UnityBleEventDispatcher.WriteResult.UNKNOWN)
                        }
                    } else {
                        if(char.setValue(value) && gatt.writeCharacteristic(char)) {
                            unityEventDispatcher.notifyOnWrite(from = characteristicUUID, result = UnityBleEventDispatcher.WriteResult.OK)
                        } else {
                            UnityLogger.e("Write failed.")
                            unityEventDispatcher.notifyOnWrite(from = characteristicUUID, result = UnityBleEventDispatcher.WriteResult.UNKNOWN)
                        }
                    }
                }
                PermissionService.PermissionResult.LocationServiceDisabled -> unityEventDispatcher.notifyOnWrite(from = characteristicUUID, result = UnityBleEventDispatcher.WriteResult.PERMISSION_DENIED)
                PermissionService.PermissionResult.SomePermissionsDined -> unityEventDispatcher.notifyOnWrite(from = characteristicUUID, result = UnityBleEventDispatcher.WriteResult.PERMISSION_DENIED)
            }
        }

        return UnityBleEventDispatcher.WriteResult.OK.ordinal
    }

    @SuppressLint("MissingPermission")
    fun subscribe(characteristicUUID: String, serviceUUID: String, peripheralUUID: String):Int {
        val gatt = connectedDevices[peripheralUUID]
            ?: return UnityBleEventDispatcher.SubscribeResult.DEVICE_NOT_FOUND.ordinal

        val sUUID = try {
            UUID.fromString(serviceUUID)
        } catch (e: IllegalArgumentException) {
            UnityLogger.e("Invalid service UUID $serviceUUID")
            return UnityBleEventDispatcher.SubscribeResult.UNKNOWN.ordinal
        }

        val cUUID = try {
            UUID.fromString(characteristicUUID)
        } catch (e: IllegalArgumentException) {
            UnityLogger.e("Invalid characteristic UUID $characteristicUUID")
            return UnityBleEventDispatcher.SubscribeResult.UNKNOWN.ordinal
        }

        val char = gatt.getService(sUUID).getCharacteristic(cUUID)
        if(!char.isSupportSubscribeOperation()) {
            return UnityBleEventDispatcher.SubscribeResult.OPERATION_NOT_SUPPORTED.ordinal
        }

        permissionService.ensurePermissionsWithResult(permissionService.requiredPermissionsForConnect()
        ) {
            result ->
            when(result) {
                PermissionService.PermissionResult.ReadyForUse -> {
                    gatt.setCharacteristicNotification(char, true)
                }
                PermissionService.PermissionResult.LocationServiceDisabled -> {
                    unityEventDispatcher.notifyOnSubscribe(from = char, value = "", result = UnityBleEventDispatcher.SubscribeResult.PERMISSION_DENIED)
                }
                PermissionService.PermissionResult.SomePermissionsDined -> {
                    unityEventDispatcher.notifyOnSubscribe(from = char, value = "", result = UnityBleEventDispatcher.SubscribeResult.PERMISSION_DENIED)
                }
            }
        }

        return UnityBleEventDispatcher.SubscribeResult.OK.ordinal
    }

    private val scanCallback = object: ScanCallback() {
        override fun onScanResult(callbackType: Int, result: ScanResult?) {
            if(result == null) {
                return
            }
            if(foundDevices.containsKey(result.device.address)) {
                UnityLogger.d("Found device ${result.device.address} : ${result.rssi}. but already we found.")
                return
            }
            foundDevices[result.device.address] = result.device
            unityEventDispatcher.notifyOnFoundDevice(result.device, result.rssi)
        }
    }

    private val gattCallback = object: BluetoothGattCallback() {
        @SuppressLint("MissingPermission")
        override fun onConnectionStateChange(gatt: BluetoothGatt?, status: Int, newState: Int) {
            if(gatt == null) {
                return
            }
            when(newState) {
                BluetoothProfile.STATE_CONNECTED -> {
                    if(connectedDevices.containsKey(gatt.device.address)) {
                        return
                    }
                    connectedDevices[gatt.device.address] = gatt
                    unityEventDispatcher.notifyResultForInvokeConnect(UnityBleEventDispatcher.ConnectResult.OK)
                    unityEventDispatcher.notifyOnConnectedDevice(gatt)
                }
                BluetoothProfile.STATE_DISCONNECTED -> {
                    connectedDevices[gatt.device.address]?.close()
                    connectedDevices.remove(gatt.device.address)
                }
            }
        }

        override fun onServicesDiscovered(gatt: BluetoothGatt?, status: Int) {
            if(gatt == null) {
                UnityLogger.w("Service discovered called but gatt is null")
                unityEventDispatcher.notifyResultForDiscoveryService(UnityBleEventDispatcher.DiscoverServiceResult.UNKNOWN)
                return
            }
            if(status == BluetoothGatt.GATT_SUCCESS) {
                unityEventDispatcher.notifyResultForDiscoveryService(UnityBleEventDispatcher.DiscoverServiceResult.OK)
                for (s in gatt.services) {
                    unityEventDispatcher.notifyOnServiceDiscovered(s, gatt.device.address)
                }
            } else {
                UnityLogger.e("Discover service failed. Status:$status")
                unityEventDispatcher.notifyResultForDiscoveryService(UnityBleEventDispatcher.DiscoverServiceResult.UNKNOWN)
            }
        }

        override fun onCharacteristicRead(
            gatt: BluetoothGatt,
            characteristic: BluetoothGattCharacteristic,
            value: ByteArray,
            status: Int
        ) {
            if (status == BluetoothGatt.GATT_SUCCESS) {
                unityEventDispatcher.notifyOnRead(characteristic, value.toBase64String(), UnityBleEventDispatcher.ReadResult.OK)
            } else {
                UnityLogger.e("Read failed from ${characteristic.uuid} status:$status")
                unityEventDispatcher.notifyOnRead(characteristic, value.toBase64String(), UnityBleEventDispatcher.ReadResult.UNKNOWN)
            }
        }

        override fun onCharacteristicChanged(
            gatt: BluetoothGatt,
            characteristic: BluetoothGattCharacteristic,
            value: ByteArray
        ) {
            unityEventDispatcher.notifyOnSubscribe(characteristic, value.toBase64String(), UnityBleEventDispatcher.SubscribeResult.OK)
        }
    }
}