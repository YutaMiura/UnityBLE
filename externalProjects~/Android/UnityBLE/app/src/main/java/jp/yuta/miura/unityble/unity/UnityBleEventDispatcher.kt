package jp.yuta.miura.unityble.unity

import android.annotation.SuppressLint
import android.bluetooth.BluetoothDevice
import android.bluetooth.BluetoothGatt
import android.bluetooth.BluetoothGattCharacteristic
import android.bluetooth.BluetoothGattService
import android.os.Handler
import android.os.Looper
import jp.yuta.miura.unityble.dto.BleDeviceDTO
import jp.yuta.miura.unityble.dto.BleServiceDTO
import jp.yuta.miura.unityble.dto.SubscribeResponse
import jp.yuta.miura.unityble.dto.ReadResponse
import jp.yuta.miura.unityble.dto.WriteResponse
import jp.yuta.miura.unityble.dto.toDto
import kotlinx.serialization.json.Json
import kotlinx.serialization.serializer

class UnityBleEventDispatcher {
    companion object {
        private const val GAME_OBJ_NAME = "AndroidBleEventListener"
        private const val METHOD_NAME_ON_FOUND_DEVICE = "OnDeviceDiscovered"
        private const val METHOD_NAME_ON_CONNECTED_DEVICE = "OnConnected"
        private const val METHOD_NAME_ON_DISCONNECTED_DEVICE = "OnDisconnected"
        private const val METHOD_NAME_SCAN_RESULT = "OnScanResult"
        private const val METHOD_NAME_STOP_SCAN_RESULT = "OnStopScanResult"
        private const val METHOD_NAME_CONNECT_RESULT = "OnConnectResult"
        private const val METHOD_NAME_DISCOVERY_RESULT = "OnDiscoveryServiceResult"
        private const val METHOD_NAME_ON_SERVICE_DISCOVERED = "OnServiceDiscovered"
        private const val METHOD_NAME_ON_READ = "OnReadCharacteristic"
        private const val METHOD_NAME_ON_WRITE = "OnWriteCharacteristic"
        private const val METHOD_NAME_ON_SUBSCRIBED = "OnSubscribed"
        private const val METHOD_NAME_ON_UNSUBSCRIBED = "OnUnsubscribed"
        private const val METHOD_NAME_ON_CLEAR_FOUND_DEVICES = "OnClearFoundDevices"
    }

    private val handler = Handler(Looper.getMainLooper())

    enum class ConnectResult {
        OK, DEVICE_NOT_FOUND, ALREADY_CONNECTED, PERMISSION_DENIED, UNKNOWN
    }
    enum class ScanResult {
        OK, BLE_FEATURE_NOT_FOUND, LOCATION_SERVICE_DISABLED, PERMISSION_DENIED, UNKNOWN
    }

    enum class DiscoverServiceResult {
        OK, NOT_CONNECTED, PERMISSION_DENIED, UNKNOWN
    }

    enum class ReadResult {
        OK, DEVICE_NOT_FOUND, OPERATION_NOT_SUPPORTED, PERMISSION_DENIED, UNKNOWN
    }

    enum class WriteResult {
        OK, DEVICE_NOT_FOUND, OPERATION_NOT_SUPPORTED, PERMISSION_DENIED, UNKNOWN
    }

    enum class SubscribeResult {
        OK, DEVICE_NOT_FOUND, OPERATION_NOT_SUPPORTED, PERMISSION_DENIED, UNKNOWN
    }

    fun notifyResultForInvokeScan(result: ScanResult) {
        handler.post {
            UnityFacade.sendToUnity(
                GAME_OBJ_NAME,
                METHOD_NAME_SCAN_RESULT,
                result.ordinal.toString()
            )
        }
    }

    fun notifyResultForInvokeStopScan(result: ScanResult) {
        handler.post {
            UnityFacade.sendToUnity(
                GAME_OBJ_NAME,
                METHOD_NAME_STOP_SCAN_RESULT,
                result.ordinal.toString()
            )
        }
    }

    fun notifyResultForInvokeConnect(result: ConnectResult) {
        handler.post {
            UnityFacade.sendToUnity(
                GAME_OBJ_NAME,
                METHOD_NAME_CONNECT_RESULT,
                result.ordinal.toString()
            )
        }
    }

    fun notifyResultForDiscoveryService(result: DiscoverServiceResult) {
        handler.post {
            UnityFacade.sendToUnity(
                GAME_OBJ_NAME,
                METHOD_NAME_DISCOVERY_RESULT,
                result.ordinal.toString()
            )
        }
    }

    fun notifyOnConnectedDevice(device: BluetoothGatt) {
        val dto = device.toDto(-70)
        val str = Json.encodeToString(serializer<BleDeviceDTO>(), dto)
        handler.post{
            UnityFacade.sendToUnity(GAME_OBJ_NAME, METHOD_NAME_ON_CONNECTED_DEVICE, str)
        }
    }

    fun notifyOnDisconnectDevice(device: BluetoothGatt) {
        val dto = device.toDto(-70)
        val str = Json.encodeToString(serializer<BleDeviceDTO>(), dto)
        handler.post{
            UnityFacade.sendToUnity(GAME_OBJ_NAME, METHOD_NAME_ON_DISCONNECTED_DEVICE, str)
        }
    }
    
    fun notifyOnClearFoundDevices() {
        handler.post {
            UnityFacade.sendToUnity(GAME_OBJ_NAME, METHOD_NAME_ON_CLEAR_FOUND_DEVICES, "cleared")
        }
    }

    @SuppressLint("MissingPermission")
    fun notifyOnFoundDevice(device: BluetoothDevice, rssi: Int) {
        val dto = device.toDto(rssi)
        val str = Json.encodeToString(serializer<BleDeviceDTO>(), dto)
        UnityLogger.d("Device found $str")
        handler.post {
            UnityFacade.sendToUnity(GAME_OBJ_NAME, METHOD_NAME_ON_FOUND_DEVICE, str)
        }
    }

    fun notifyOnServiceDiscovered(service: BluetoothGattService, deviceAddress: String) {
        val dto = service.toDto(deviceAddress)
        val str = Json.encodeToString(serializer<BleServiceDTO>(), dto)
        handler.post{
            UnityFacade.sendToUnity(GAME_OBJ_NAME, METHOD_NAME_ON_SERVICE_DISCOVERED, str)
        }
    }

    fun notifyOnRead(from: BluetoothGattCharacteristic, value: String, status: ReadResult) {
        val dto = ReadResponse(from = from.uuid.toString(), value = value, status = status.ordinal)
        val str = Json.encodeToString(serializer<ReadResponse>(), dto)
        handler.post{
            UnityFacade.sendToUnity(GAME_OBJ_NAME, METHOD_NAME_ON_READ, str)
        }
    }

    fun notifyOnRead(from: String, value: String, status: ReadResult) {
        val dto = ReadResponse(from = from, value = value, status = status.ordinal)
        val str = Json.encodeToString(serializer<ReadResponse>(), dto)
        handler.post{
            UnityFacade.sendToUnity(GAME_OBJ_NAME, METHOD_NAME_ON_READ, str)
        }
    }

    fun notifyOnWrite(from: BluetoothGattCharacteristic, result: WriteResult) {
        val dto = WriteResponse(from = from.uuid.toString(), status = result.ordinal)
        val str = Json.encodeToString(serializer<WriteResponse>(), dto)
        handler.post{
            UnityFacade.sendToUnity(GAME_OBJ_NAME, METHOD_NAME_ON_WRITE, str)
        }
    }

    fun notifyOnWrite(from: String, result: WriteResult) {
        val dto = WriteResponse(from = from, status = result.ordinal)
        val str = Json.encodeToString(serializer<WriteResponse>(), dto)
        handler.post{
            UnityFacade.sendToUnity(GAME_OBJ_NAME, METHOD_NAME_ON_WRITE, str)
        }
    }

    fun notifyOnSubscribe(from: BluetoothGattCharacteristic, value:String, result: SubscribeResult) {
        val dto = SubscribeResponse(from = from.uuid.toString(), value = value, status = result.ordinal)
        val str = Json.encodeToString(serializer<SubscribeResponse>(), dto)
        handler.post{
            UnityFacade.sendToUnity(GAME_OBJ_NAME, METHOD_NAME_ON_SUBSCRIBED, str)
        }
    }

    fun notifyOnUnSubscribe(from: BluetoothGattCharacteristic, result: SubscribeResult){
        val dto = SubscribeResponse(from = from.uuid.toString(), value = "", status = result.ordinal)
        val str = Json.encodeToString(serializer<SubscribeResponse>(), dto)
        handler.post {
            UnityFacade.sendToUnity(GAME_OBJ_NAME, METHOD_NAME_ON_UNSUBSCRIBED, str)
        }
    }
}