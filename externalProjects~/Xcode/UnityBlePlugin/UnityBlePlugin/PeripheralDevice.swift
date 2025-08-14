import Foundation
import CoreBluetooth

class PeripheralDevice: NSObject {
    let peripheral: CBPeripheral
    private let centralManager: CBCentralManager
    var rssi: NSNumber
    var connectionState: PeripheralConnectionState = .disconnected

    init(peripheral: CBPeripheral, rssi: NSNumber, centralManager: CBCentralManager) {
        self.peripheral = peripheral
        self.rssi = rssi
        self.centralManager = centralManager
        super.init()
        self.peripheral.delegate = self
    }

    func connect() -> Int {
        if connectionState != .disconnected {
            UnityLogger.logError("Peripheral is already connected or connecting.")
            return -1
        }
        centralManager.connect(peripheral, options: nil)
        UnityLogger.log("Connecting to peripheral: \(peripheral.name ?? "Unknown")")
        connectionState = .connecting
        return 0
    }

    func disconnect() -> Int {
        if connectionState != .connected {
            UnityLogger.logError("Peripheral is not connected.")
            return -1
        }
        centralManager.cancelPeripheralConnection(peripheral)
        UnityLogger.log("Disconnecting from peripheral: \(peripheral.name ?? "Unknown")")
        connectionState = .disconnecting
        return 0
    }

    func discoverServices() {
        guard connectionState == .connected else {
            UnityLogger.logError("Peripheral is not connected.")
            return
        }
        peripheral.discoverServices(nil)
        UnityLogger.log("Discovering services for peripheral: \(peripheral.name ?? "Unknown")")
    }

    func getServices() -> [CBService]? {
        let services = peripheral.services
        return services
    }

    func getCharacteristic(for serviceUUID: CBUUID, characteristicUUID: CBUUID) -> CBCharacteristic? {
        guard let service = peripheral.services?.first(where: { $0.uuid == serviceUUID }) else {
            return nil
        }
        return service.characteristics?.first(where: { $0.uuid == characteristicUUID })
    }

    func getCharacteristics(for serviceUUID: CBUUID) -> [CBCharacteristic]? {
        guard let service = peripheral.services?.first(where: { $0.uuid == serviceUUID }) else {
            return nil
        }
        return service.characteristics
    }

    func findCharacteristic(byUUID uuid: CBUUID) -> CBCharacteristic? {
        for service in peripheral.services ?? [] {
            if let characteristic = service.characteristics?.first(where: { $0.uuid == uuid }) {
                return characteristic
            }
        }
        return nil
    }

    func subscribeToCharacteristic(_ characteristic: CBCharacteristic) -> Int{
        if characteristic.properties.contains(.notify) || characteristic.properties.contains(.indicate) {
            peripheral.setNotifyValue(true, for: characteristic)
            UnityLogger.log("Subscribed to characteristic: \(characteristic.uuid)")
            return 0
        } else {
            UnityLogger.logError("Characteristic \(characteristic.uuid) does not support notifications or indications.")
            return -1
        }
    }

    func unsubscribeFromCharacteristic(_ characteristic: CBCharacteristic) -> Int {
        if characteristic.properties.contains(.notify) || characteristic.properties.contains(.indicate) {
            peripheral.setNotifyValue(false, for: characteristic)
            UnityLogger.log("Unsubscribed from characteristic: \(characteristic.uuid)")
            return 0
        } else {
            UnityLogger.logError("Characteristic \(characteristic.uuid) does not support notifications or indications.")
            return -1
        }
    }

    func readCharacteristic(_ characteristic: CBCharacteristic) -> Int{
        guard characteristic.properties.contains(.read) else {
            UnityLogger.logError("Characteristic \(characteristic.uuid) does not support reading.")
            return -1
        }
        peripheral.readValue(for: characteristic)
        UnityLogger.log("Reading value for characteristic: \(characteristic.uuid)")
        return 0
    }

    func writeCharacteristic(_ characteristic: CBCharacteristic, value: Data) -> Int {
        guard characteristic.properties.contains(.write) || characteristic.properties.contains(.writeWithoutResponse) else {
            UnityLogger.logError("Characteristic \(characteristic.uuid) does not support writing.")
            BleErrorDTO.InvalidOperation(error: "Characteristic \(characteristic.uuid) does not support writing.").notifyToUnity()
            return -1
        }
        if characteristic.properties.contains(.writeWithoutResponse) {
            peripheral.writeValue(value, for: characteristic, type: .withoutResponse)
            UnityDelegates.OnWriteCharacteristicCompleted?(characteristic.uuid.uuidString, 0, "Successfully wrote value to characteristic \(characteristic.uuid)")
        } else {
            peripheral.writeValue(value, for: characteristic, type: .withResponse)
        }
        UnityLogger.log("Writing value to characteristic: \(characteristic.uuid)")
        return 0
    }

}

extension PeripheralDevice: CBPeripheralDelegate {
    func peripheral(_ peripheral: CBPeripheral, didDiscoverServices error: Error?) {
        if let error = error {
            UnityLogger.logError("Error discovering services: \(error.localizedDescription)")
            UnityDelegates.OnBleErrorDetected?("Error discovering services: \(error.localizedDescription)")
            return
        }
        guard let services = peripheral.services else { return }
        UnityLogger.log("Discovered services: \(services.map { $0.uuid })")
        peripheral.services?.forEach { service in
            let dto = ServiceDTO(service: service)
            guard let json = dto.toJson() else { return }
            UnityDelegates.OnDiscoveredServices?(json)
            peripheral.discoverCharacteristics(nil, for: service)
        }
    }

    func peripheral(_ peripheral: CBPeripheral, didDiscoverCharacteristicsFor service: CBService, error: Error?) {
        if let error = error {
            UnityLogger.logError("Error discovering characteristics for service \(service.uuid): \(error.localizedDescription)")
            UnityDelegates.OnBleErrorDetected?("Error discovering characteristics for service \(service.uuid): \(error.localizedDescription)")
            return
        }
        guard let characteristics = service.characteristics else { return }
        UnityLogger.log("Discovered characteristics for service \(service.uuid): \(characteristics.map { $0.uuid })")
        characteristics.forEach { characteristic in
            let dto = CharacteristicDTO(characteristic: characteristic)
            guard let json = dto.toJson() else { return }
            UnityDelegates.OnDiscoveredCharacteristics?(json)
        }
    }

    func peripheral(_ peripheral: CBPeripheral, didUpdateValueFor characteristic: CBCharacteristic, error: Error?) {
        if let error = error {
            UnityLogger.logError("Error updating value for characteristic \(characteristic.uuid): \(error.localizedDescription)")
            UnityDelegates.OnBleErrorDetected?("Error updating value for characteristic \(characteristic.uuid): \(error.localizedDescription)")
            return
        }
        guard let value = characteristic.value else { return }
        UnityLogger.log("Updated value for characteristic \(characteristic.uuid): \(value)")
        UnityDelegates.OnDataReceived?(
            characteristic.uuid.uuidString,
            value.base64EncodedString()
        )
    }

    func peripheral(_ peripheral: CBPeripheral, didWriteValueFor characteristic: CBCharacteristic, error: Error?) {
        if let error = error {
            UnityDelegates.OnWriteCharacteristicCompleted?(characteristic.uuid.uuidString, -1, error.localizedDescription)
            return
        }
        UnityLogger.log("Successfully wrote value for characteristic \(characteristic.uuid)")
        UnityDelegates.OnWriteCharacteristicCompleted?(characteristic.uuid.uuidString, 0, "Successfully wrote value for characteristic \(characteristic.uuid)")
    }

    func peripheral(_ peripheral: CBPeripheral, didReadRSSI RSSI: NSNumber, error: (any Error)?) {
        if let error = error {
            UnityLogger.logError("Error reading RSSI for peripheral \(peripheral.name ?? "Unknown"): \(error.localizedDescription)")
            UnityDelegates.OnReadRSSICompleted?(-1, error.localizedDescription)
            return
        }
        UnityLogger.log("Read RSSI for peripheral \(peripheral.name ?? "Unknown"): \(RSSI)")
        UnityDelegates.OnReadRSSICompleted?(0, "Read RSSI for peripheral \(peripheral.name ?? "Unknown"): \(RSSI)")
        self.rssi = RSSI
    }
}

enum PeripheralConnectionState {
    case disconnected
    case connecting
    case connected
    case disconnecting

    var description: String {
        switch self {
        case .disconnected: return "Disconnected"
        case .connecting: return "Connecting"
        case .connected: return "Connected"
        case .disconnecting: return "Disconnecting"
        }
    }
}