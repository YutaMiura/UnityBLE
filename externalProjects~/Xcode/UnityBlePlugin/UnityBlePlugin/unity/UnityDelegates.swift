import Foundation
import CoreBluetooth

public class UnityDelegates: NSObject {

    public static var OnPeripheralDiscovered: ((String) -> Void)?

    public static var OnPeripheralUpdated: ((String) -> Void)?

    public static var OnDiscoveredPeripheralCleared: (() -> Void)?

    public static var OnPeripheralConnected: ((String) -> Void)?

    public static var OnPeripheralDisconnected: ((String) -> Void)?

    public static var OnBleErrorDetected: ((String) -> Void)?

    public static var OnBleStateChanged: ((Int) -> Void)?

    public static var OnDiscoveredServices: ((String) -> Void)?

    public static var OnDiscoveredCharacteristics: ((String) -> Void)?

    public static var OnWriteCharacteristicCompleted: ((String, Int, String) -> Void)?

    public static var OnReadRSSICompleted: ((Int, String) -> Void)?

    public static var OnDataReceived: ((String, String) -> Void)?

    static func notifyPeripheralDiscovered(peripheral: CBPeripheral, rssi: NSNumber, advertisementData: [String: Any]? = nil) {
        let dto = PeripheralDTO(
            peripheral: peripheral,
            rssi: rssi,
            services: peripheral.services,
            advertisementData: advertisementData)
        guard let json = dto.toJson() else { return }
        OnPeripheralDiscovered?(json)
    }

    static func notifyPeripheralUpdated(peripheral: CBPeripheral, rssi: NSNumber, advertisementData: [String: Any]? = nil) {
        let dto = PeripheralDTO(
            peripheral: peripheral,
            rssi: rssi,
            services: peripheral.services,
            advertisementData: advertisementData)
        guard let json = dto.toJson() else { return }
        OnPeripheralUpdated?(json)
    }
}