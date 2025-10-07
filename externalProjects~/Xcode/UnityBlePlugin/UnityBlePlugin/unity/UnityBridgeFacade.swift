import Foundation
import CoreBluetooth

@objc public class UnityBridgeFacade: NSObject {
    @objc public static let shared = UnityBridgeFacade()

    private let bleScanner: BleScanner

    private override init() {
        bleScanner = BleScanner()
        super.init()
        UnityLogger.log("UnityBridgeFacade initialized.")
    }

    @objc public func isScanning() -> Bool {
        return bleScanner.isScanning()
    }

    @objc public func getBleState() -> Int {
        return bleScanner.getState()
    }

    @objc public func startScanning(for services: [CBUUID]?, nameFilter: String? = nil) -> Int {
        return bleScanner.startScan(services, nameFilter: nameFilter)
    }

    @objc public func stopScanning() {
        bleScanner.stopScan()
    }

    @objc public func connectToPeripheral(withUUID uuid: UUID) -> Int {
        return bleScanner.connect(peripheralUUID: uuid)
    }

    @objc public func disconnectFromPeripheral(withUUID uuid: UUID) -> Int {
        guard let peripheral = bleScanner.findPeripheral(byUUID: uuid) else {
            UnityLogger.logError("Peripheral with UUID \(uuid) not found.")
            return -1
        }
        return peripheral.disconnect()
    }

    @objc public func discoverServices(forPeripheral uuid: UUID) -> Int {
        guard let peripheral = bleScanner.findPeripheral(byUUID: uuid) else {
            UnityLogger.logError("Peripheral with UUID \(uuid) not found.")
            return -1
        }
        peripheral.discoverServices()
        return 0
    }

    @objc public func writeValue(_ value: Data, toCharacteristic characteristicUUID: CBUUID, forService serviceUUID: CBUUID, ofPeripheral peripheralUUID: UUID) -> Int {
        guard let peripheral = bleScanner.findPeripheral(byUUID: peripheralUUID) else {
            UnityLogger.logError("Peripheral with UUID \(peripheralUUID) not found.")
            return -1
        }
        guard let characteristic = peripheral.getCharacteristic(for: serviceUUID, characteristicUUID: characteristicUUID) else {
            UnityLogger.logError("Characteristic with UUID \(characteristicUUID) not found in service \(serviceUUID).")
            return -1
        }
        return peripheral.writeCharacteristic(characteristic, value: value)
    }

    @objc public func readCharacteristic(withUUID characteristicUUID: CBUUID, forService serviceUUID: CBUUID, ofPeripheral peripheralUUID: UUID) -> Int {
        guard let peripheral = bleScanner.findPeripheral(byUUID: peripheralUUID) else {
            UnityLogger.logError("Peripheral with UUID \(peripheralUUID) not found.")
            return -1
        }
        guard let characteristic = peripheral.getCharacteristic(for: serviceUUID, characteristicUUID: characteristicUUID) else {
            UnityLogger.logError("Characteristic with UUID \(characteristicUUID) not found in service \(serviceUUID).")
            return -1
        }
        return peripheral.readCharacteristic(characteristic)
    }

    @objc public func subscribeToCharacteristic(withUUID characteristicUUID: CBUUID, forService serviceUUID: CBUUID, ofPeripheral peripheralUUID: UUID) -> Int {
        guard let peripheral = bleScanner.findPeripheral(byUUID: peripheralUUID) else {
            UnityLogger.logError("Peripheral with UUID \(peripheralUUID) not found.")
            return -1
        }
        guard let characteristic = peripheral.getCharacteristic(for: serviceUUID, characteristicUUID: characteristicUUID) else {
            UnityLogger.logError("Characteristic with UUID \(characteristicUUID) not found in service \(serviceUUID).")
            return -1
        }
        return peripheral.subscribeToCharacteristic(characteristic)
    }

    @objc public func unsubscribeFromCharacteristic(withUUID characteristicUUID: CBUUID, forService serviceUUID: CBUUID, ofPeripheral peripheralUUID: UUID) -> Int {
        guard let peripheral = bleScanner.findPeripheral(byUUID: peripheralUUID) else {
            UnityLogger.logError("Peripheral with UUID \(peripheralUUID) not found.")
            return -1
        }
        guard let characteristic = peripheral.getCharacteristic(for: serviceUUID, characteristicUUID: characteristicUUID) else {
            UnityLogger.logError("Characteristic with UUID \(characteristicUUID) not found in service \(serviceUUID).")
            return -1
        }
        return peripheral.unsubscribeFromCharacteristic(characteristic)
    }

    @objc public func registerOnPeripheralDiscovered(_ callback: @escaping (String) -> Void) {
        UnityDelegates.OnPeripheralDiscovered = callback
    }

    @objc public func registerOnPeripheralConnected(_ callback: @escaping (String) -> Void) {
        UnityDelegates.OnPeripheralConnected = callback
    }

    @objc public func registerOnPeripheralDisconnected(_ callback: @escaping (String) -> Void) {
        UnityDelegates.OnPeripheralDisconnected = callback
    }

    @objc public func registerOnBleErrorDetected(_ callback: @escaping (String) -> Void) {
        UnityDelegates.OnBleErrorDetected = callback
    }

    @objc public func registerOnBleStateChanged(_ callback: @escaping (Int) -> Void) {
        UnityDelegates.OnBleStateChanged = callback
    }

    @objc public func registerOnDiscoveredPeripheralCleared(_ callback: @escaping () -> Void) {
        UnityDelegates.OnDiscoveredPeripheralCleared = callback
    }

    @objc public func registerOnDiscoveredServices(_ callback: @escaping (String) -> Void) {
        UnityDelegates.OnDiscoveredServices = callback
    }

    @objc public func registerOnDiscoveredCharacteristics(_ callback: @escaping (String) -> Void) {
        UnityDelegates.OnDiscoveredCharacteristics = callback
    }

    @objc public func registerOnLog(_ callback: @escaping (String) -> Void) {
        UnityLogger.OnLog = callback
    }

    @objc public func registerOnWriteCharacteristicCompleted(_ callback: @escaping (String, Int, String) -> Void) {
        UnityDelegates.OnWriteCharacteristicCompleted = callback
    }

    @objc public func registerOnReadRSSICompleted(_ callback: @escaping (Int, String) -> Void) {
        UnityDelegates.OnReadRSSICompleted = callback
    }

    @objc public func registerOnDataReceived(_ callback: @escaping (String, String) -> Void) {
        UnityDelegates.OnDataReceived = callback
    }
}
