package jp.yuta.miura.unityble

import android.annotation.SuppressLint
import android.app.Activity
import android.bluetooth.BluetoothAdapter
import android.bluetooth.BluetoothDevice
import android.bluetooth.BluetoothGatt
import android.bluetooth.BluetoothGattCallback
import android.bluetooth.BluetoothGattCharacteristic
import android.bluetooth.BluetoothGattDescriptor
import android.bluetooth.BluetoothManager
import android.bluetooth.BluetoothProfile
import android.bluetooth.BluetoothStatusCodes
import android.bluetooth.le.ScanCallback
import android.bluetooth.le.ScanFilter
import android.bluetooth.le.ScanResult
import android.bluetooth.le.ScanSettings
import android.content.Context
import android.content.pm.PackageManager
import android.os.Build
import android.os.ParcelUuid
import com.unity3d.player.UnityPlayer
import jp.yuta.miura.unityble.unity.UnityBleEventDispatcher
import jp.yuta.miura.unityble.unity.UnityLogger
import java.util.UUID
import java.util.concurrent.ConcurrentHashMap


class BleManager private constructor(private val activity: Activity) {
    private val bleManager : BluetoothManager = activity.getSystemService(Context.BLUETOOTH_SERVICE) as BluetoothManager
    private val bleAdapter : BluetoothAdapter? = bleManager.adapter
    private val permissionService: PermissionService = PermissionService(activity)
    private val unityEventDispatcher = UnityBleEventDispatcher()
    private val foundDevices: ConcurrentHashMap<String, BluetoothDevice> = ConcurrentHashMap()
    private val connectedDevices: ConcurrentHashMap<String, BluetoothGatt> = ConcurrentHashMap()

    companion object  {
        @JvmStatic
        val instance: BleManager by lazy {
            BleManager(UnityPlayer.currentActivity)
        }

        val CCCD: UUID = UUID.fromString("00002902-0000-1000-8000-00805f9b34fb")
    }

    @SuppressLint("MissingPermission")
    fun startBleScan(names: Array<String>, serviceUUIDs: Array<String>) {
        UnityLogger.d("Call startBleScan. names:${names.joinToString()} services:${serviceUUIDs.joinToString()}")
        if(!isBleSupported()) {
            unityEventDispatcher.notifyResultForInvokeScan(UnityBleEventDispatcher.ScanResult.BLE_FEATURE_NOT_FOUND)
            return
        }
        val filters = ArrayList<ScanFilter>()
        for (name in names) {
            if(name.isNotBlank()) {
                filters.add(ScanFilter.Builder().setDeviceName(name).build())
            }
        }
        for (service in serviceUUIDs) {
            if(service.isNotBlank()) {
                val uuid = ParcelUuid(UUID.fromString(service))
                filters.add(ScanFilter.Builder().setServiceUuid(uuid).build())
            }
        }
        val setting = ScanSettings.Builder().build()

        permissionService.ensurePermissionsWithResult(permissionService.requiredPermissionsForScan()
        ) { result ->
            UnityLogger.d("Scan result $result")
            when (result) {
                PermissionService.PermissionResult.ReadyForUse -> {
                    try {
                        foundDevices.clear() // Clear discovered devices when stopping scan
                        unityEventDispatcher.notifyOnClearFoundDevices()
                        if(filters.isEmpty()) {
                            UnityLogger.d("Start scan. with no filter.")
                            bleAdapter?.bluetoothLeScanner?.startScan(scanCallback)
                        } else {
                            val filterDescription = filters.joinToString { filter ->
                                when {
                                    filter.deviceName != null -> "name:${filter.deviceName}"
                                    filter.serviceUuid != null -> "service:${filter.serviceUuid}"
                                    else -> "unknown"
                                }
                            }
                            UnityLogger.d("Start scan. with filters: [$filterDescription] Length=>${filters.count()}")
                            bleAdapter?.bluetoothLeScanner?.startScan(filters, setting, scanCallback)
                        }
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
        UnityLogger.d("Call stopBleScan.")
        if(!isBleSupported()) {
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
        return pm.hasSystemFeature(PackageManager.FEATURE_BLUETOOTH_LE) && bleAdapter != null && bleAdapter.isEnabled
    }

    @SuppressLint("MissingPermission")
    fun connect(address: String) {
        UnityLogger.d("Call connect to $address.")
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
        UnityLogger.d("Call disconnect with $address.")
        connectedDevices[address]?.close()
    }

    @SuppressLint("MissingPermission")
    fun discoveryServices(address: String) {
        UnityLogger.d("Call discoverService with $address.")
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
        UnityLogger.d("Call read with device:$peripheralUUID service:$serviceUUID char:$characteristicUUID.")
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
        UnityLogger.d("Call subscribe with device:$peripheralUUID service:$serviceUUID char:$characteristicUUID.")
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
            UnityLogger.d("Check permission for subscribe:$result.")
            when(result) {
                PermissionService.PermissionResult.ReadyForUse -> {
                    UnityLogger.d("Start subscribe for $characteristicUUID")
                    val success = gatt.setCharacteristicNotification(char, true)
                    if(!success) {
                        unityEventDispatcher.notifyOnSubscribe(from = char, value = "", result = UnityBleEventDispatcher.SubscribeResult.UNKNOWN)
                        UnityLogger.d("Start subscribe for $characteristicUUID failed.")
                    } else {
                        UnityLogger.d("Found descriptors ${char.descriptors.joinToString { it.uuid.toString()} } for $characteristicUUID")
                        val descriptor = char.getDescriptor(CCCD)
                        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
                            when (gatt.writeDescriptor(descriptor, BluetoothGattDescriptor.ENABLE_NOTIFICATION_VALUE)) {

                                BluetoothStatusCodes.ERROR_GATT_WRITE_NOT_ALLOWED -> {
                                    UnityLogger.d("Write ${BluetoothGattDescriptor.ENABLE_NOTIFICATION_VALUE} to descriptor failed. cause NOT_ALLOWED")
                                }

                                BluetoothStatusCodes.ERROR_GATT_WRITE_REQUEST_BUSY -> {
                                    UnityLogger.d("Write ${BluetoothGattDescriptor.ENABLE_NOTIFICATION_VALUE} to descriptor failed. cause BUSY")
                                }

                                BluetoothStatusCodes.ERROR_MISSING_BLUETOOTH_CONNECT_PERMISSION -> {
                                    UnityLogger.d("Write ${BluetoothGattDescriptor.ENABLE_NOTIFICATION_VALUE} to descriptor failed. cause MISSING_PERMISSION")
                                }

                                BluetoothStatusCodes.ERROR_PROFILE_SERVICE_NOT_BOUND -> {
                                    UnityLogger.d("Write ${BluetoothGattDescriptor.ENABLE_NOTIFICATION_VALUE} to descriptor failed. cause SERVICE_NOT_BOUND")
                                }

                                BluetoothStatusCodes.ERROR_UNKNOWN -> {
                                    UnityLogger.d("Write ${BluetoothGattDescriptor.ENABLE_NOTIFICATION_VALUE} to descriptor failed. cause UNKNOWN")
                                }

                                BluetoothStatusCodes.SUCCESS -> {
                                    UnityLogger.d("Write ${BluetoothGattDescriptor.ENABLE_NOTIFICATION_VALUE} to descriptor succeed.")
                                }
                            }
                        } else {
                            if ((char.properties and BluetoothGattCharacteristic.PROPERTY_INDICATE) != 0) {
                                descriptor.value = BluetoothGattDescriptor.ENABLE_INDICATION_VALUE
                            } else {
                                descriptor.value = BluetoothGattDescriptor.ENABLE_NOTIFICATION_VALUE
                            }
                            if(gatt.writeDescriptor(descriptor)) {
                                UnityLogger.d("Write ${BluetoothGattDescriptor.ENABLE_NOTIFICATION_VALUE} to descriptor succeed.")
                            } else {
                                UnityLogger.d("Write ${BluetoothGattDescriptor.ENABLE_NOTIFICATION_VALUE} to descriptor failed.")
                            }
                        }
                        UnityLogger.d("Start subscribe for $characteristicUUID succeed.")
                    }
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

    @SuppressLint("MissingPermission")
    fun unsubscribe(characteristicUUID: String, serviceUUID: String, peripheralUUID: String): Int {
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
                    val success = gatt.setCharacteristicNotification(char, false)
                    if(!success) {
                        unityEventDispatcher.notifyOnUnSubscribe(from = char, result = UnityBleEventDispatcher.SubscribeResult.UNKNOWN)
                    } else {
                        val descriptor = char.getDescriptor(CCCD)
                        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
                            gatt.writeDescriptor(descriptor, BluetoothGattDescriptor.DISABLE_NOTIFICATION_VALUE)
                        } else {
                            descriptor.value = BluetoothGattDescriptor.DISABLE_NOTIFICATION_VALUE
                            gatt.writeDescriptor(descriptor)
                        }
                    }
                }
                PermissionService.PermissionResult.LocationServiceDisabled -> {
                    unityEventDispatcher.notifyOnUnSubscribe(from = char, result = UnityBleEventDispatcher.SubscribeResult.PERMISSION_DENIED)
                }
                PermissionService.PermissionResult.SomePermissionsDined -> {
                    unityEventDispatcher.notifyOnUnSubscribe(from = char, result = UnityBleEventDispatcher.SubscribeResult.PERMISSION_DENIED)
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
            // Thread-safe check and add
            val existingDevice = foundDevices.putIfAbsent(result.device.address, result.device)
            if(existingDevice == null) {
                // Device was newly added
                unityEventDispatcher.notifyOnFoundDevice(result.device, result.rssi)
            }
        }

        override fun onBatchScanResults(results: MutableList<ScanResult>?) {
            UnityLogger.d("onBatchScanResults: received ${results?.size ?: 0} results")
            if(results.isNullOrEmpty()) {
                return
            }
            
            for(result in results) {
                // Thread-safe check and add
                val existingDevice = foundDevices.putIfAbsent(result.device.address, result.device)
                if(existingDevice == null) {
                    // Device was newly added
                    unityEventDispatcher.notifyOnFoundDevice(result.device, result.rssi)
                }
            }
        }

        override fun onScanFailed(errorCode: Int) {
            when(errorCode) {
                SCAN_FAILED_ALREADY_STARTED -> {
                    UnityLogger.e("Scan failed: Already started")
                }
                SCAN_FAILED_APPLICATION_REGISTRATION_FAILED -> {
                    UnityLogger.e("Scan failed: Application registration failed")
                }
                SCAN_FAILED_INTERNAL_ERROR -> {
                    UnityLogger.e("Scan failed: Internal error")
                }
                SCAN_FAILED_FEATURE_UNSUPPORTED -> {
                    UnityLogger.e("Scan failed: Feature unsupported")
                }
                else -> {
                    UnityLogger.e("Scan failed: Unknown error")
                }
            }
        }
    }

    private val gattCallback = object: BluetoothGattCallback() {
        @SuppressLint("MissingPermission")
        override fun onConnectionStateChange(gatt: BluetoothGatt?, status: Int, newState: Int) {
            UnityLogger.d("Connection state has been changed. $status")
            if(gatt == null) {
                return
            }
            UnityLogger.d("Connection state has been changed. $status ${gatt.device.address}")
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
                    unityEventDispatcher.notifyOnDisconnectDevice(gatt)
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

        @Deprecated("Deprecated in API level 33. but we supports API level less than 33.")
        override fun onCharacteristicRead(
            gatt: BluetoothGatt?,
            characteristic: BluetoothGattCharacteristic?,
            status: Int
        ) {
            if(gatt == null) return
            if(characteristic == null) return
            UnityLogger.d("onCharacteristicRead(less than 33) from:${characteristic.uuid} value:${characteristic.value.toBase64String()}, status:$status")
            if (status == BluetoothGatt.GATT_SUCCESS) {
                unityEventDispatcher.notifyOnRead(characteristic, characteristic.value.toBase64String(), UnityBleEventDispatcher.ReadResult.OK)
            } else {
                UnityLogger.e("Read failed from ${characteristic.uuid} status:$status")
                unityEventDispatcher.notifyOnRead(characteristic, characteristic.value.toBase64String(), UnityBleEventDispatcher.ReadResult.UNKNOWN)
            }
        }

        @Deprecated("Deprecated in API level 33. but we supports API level less than 33.")
        override fun onCharacteristicChanged(
            gatt: BluetoothGatt?,
            characteristic: BluetoothGattCharacteristic?
        ) {
            if(gatt == null)return
            if(characteristic == null)return
            UnityLogger.d("onCharacteristicChanged(less than 33) from:${characteristic.uuid} value:${characteristic.value.toBase64String()}")
            unityEventDispatcher.notifyOnSubscribe(characteristic, characteristic.value.toBase64String(), UnityBleEventDispatcher.SubscribeResult.OK)
        }

        override fun onCharacteristicRead(
            gatt: BluetoothGatt,
            characteristic: BluetoothGattCharacteristic,
            value: ByteArray,
            status: Int
        ) {
            UnityLogger.d("onCharacteristicRead from:${characteristic.uuid} value:${value.toBase64String()}, status:$status")
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
            UnityLogger.d("onCharacteristicChanged from:${characteristic.uuid} value:${value.toBase64String()}")
            unityEventDispatcher.notifyOnSubscribe(characteristic, value.toBase64String(), UnityBleEventDispatcher.SubscribeResult.OK)
        }

        override fun onDescriptorWrite(
            gatt: BluetoothGatt?,
            descriptor: BluetoothGattDescriptor?,
            status: Int
        ) {
            if(gatt == null) return
            if(descriptor == null) return
            UnityLogger.d("onDescriptorWrite to ${gatt.device.address} descriptor: ${descriptor.uuid} status:$status")
        }

        override fun onCharacteristicWrite(
            gatt: BluetoothGatt?,
            characteristic: BluetoothGattCharacteristic?,
            status: Int
        ) {
            if(gatt == null) return
            if(characteristic == null)return
            UnityLogger.d("onCharacteristicWrite to ${gatt.device.address} char:${characteristic.uuid} status:$status")
        }
    }
}