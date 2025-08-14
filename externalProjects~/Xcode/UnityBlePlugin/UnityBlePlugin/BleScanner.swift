import Foundation
import CoreBluetooth


class BleScanner: NSObject {
    private let centralManager: CBCentralManager
    private var discoveredPeripherals: [PeripheralDevice] = []
    private var nameFilter: String? = nil

    override init() {
        let options: [String: Any] = [CBCentralManagerOptionShowPowerAlertKey: true]
        centralManager = CBCentralManager(delegate: nil, queue: nil, options: options)
        super.init()
        centralManager.delegate = self
    }

    func isScanning() -> Bool {
        return centralManager.isScanning
    }

    func startScan(_ services: [CBUUID]?, nameFilter: String? = nil) -> Int {
        UnityLogger.log("Start scan with central manager state : \(centralManager.state)")
        if centralManager.isScanning {
            UnityLogger.log("Already scanning")
            return 1
        }
        if centralManager.state == .poweredOn {
            self.nameFilter = nameFilter
            discoveredPeripherals.removeAll { peripheralDevice in
                peripheralDevice.connectionState == .disconnected
            }
            UnityDelegates.OnDiscoveredPeripheralCleared?()
            centralManager.scanForPeripherals(withServices: services, options: nil)
            UnityLogger.log("Started scanning for peripherals with services: \(String(describing: services)), nameFilter: \(nameFilter ?? "none")")
            return 0
        } else if centralManager.state == .poweredOff {
            return -1
        } else if centralManager.state == .unauthorized {
            return -2
        } else if centralManager.state == .unsupported {
            return -3
        } else {
            return -4
        }
    }

    func findPeripheral(byUUID uuid: UUID) -> PeripheralDevice? {
        return discoveredPeripherals.first { $0.peripheral.identifier == uuid }
    }

    func stopScan() {
        centralManager.stopScan()
        nameFilter = nil
        UnityLogger.log("Stopped scanning for peripherals.")
    }

    func connect(peripheralUUID: UUID) -> Int {
        guard let peripheral = discoveredPeripherals.first(where: { $0.peripheral.identifier == peripheralUUID }) else {
            UnityLogger.logError("Peripheral with UUID \(peripheralUUID) not found.")
            return -1
        }
        UnityLogger.log("Connecting to peripheral: \(peripheral.peripheral.name ?? "Unknown")")
        return peripheral.connect()
    }
}

extension BleScanner: CBCentralManagerDelegate {
    func centralManagerDidUpdateState(_ central: CBCentralManager) {
        UnityLogger.log("Central Manager state updated: \(central.state)")
        UnityDelegates.OnBleStateChanged?(central.state.rawValue)
    }

    func centralManager(_ central: CBCentralManager, didDiscover peripheral: CBPeripheral, advertisementData: [String: Any], rssi RSSI: NSNumber) {
        UnityLogger.log("Discovered peripheral: \(peripheral.name ?? "Unknown") with RSSI: \(RSSI)")
        if let filter = nameFilter {
            guard let peripheralName = peripheral.name,
                  peripheralName.lowercased().contains(filter.lowercased()) else {
                UnityLogger.log("Name was filtered out: \(peripheral.name ?? "Unknown")")
                return
            }
        }

        if !discoveredPeripherals.contains(where: { $0.peripheral.identifier == peripheral.identifier }) {
            let device = PeripheralDevice(peripheral: peripheral, rssi: RSSI, centralManager: centralManager)
            discoveredPeripherals.append(device)
            UnityDelegates.notifyPeripheralDiscovered(peripheral: peripheral, rssi: RSSI)
        }
    }

    func centralManager(_ central: CBCentralManager, didConnect peripheral: CBPeripheral) {
        UnityLogger.log("Connected to peripheral: \(peripheral.name ?? "Unknown")")
        guard let device = discoveredPeripherals.first(where: { $0.peripheral.identifier == peripheral.identifier }) else {
            UnityLogger.logError("Connected peripheral not found in discovered peripherals.")
            return
        }
        device.connectionState = .connected
        let dto = PeripheralDTO(peripheral: peripheral, rssi: device.rssi, services: peripheral.services)
        guard let dtoJson = dto.toJson() else {
            UnityLogger.logError("Failed to convert PeripheralDTO to JSON.")
            BleErrorDTO.ConnectionFailed(error: "Connection succeeded but JSON conversion failed. so try disconnect from device.").notifyToUnity()
            let result = device.disconnect()
            if(result != 0) {
                UnityLogger.logError("Failed to disconnect after connection error: \(result)")
            }
            return
        }
        UnityDelegates.OnPeripheralConnected?(dtoJson)
    }

    func centralManager(_ central: CBCentralManager, didFailToConnect peripheral: CBPeripheral, error: Error?) {
        UnityLogger.log("Failed to connect to peripheral: \(peripheral.name ?? "Unknown"), error: \(error?.localizedDescription ?? "Unknown error")")
        BleErrorDTO.ConnectionFailed(error: error?.localizedDescription ?? "Unknown error").notifyToUnity()
    }

    func centralManager(_ central: CBCentralManager, didDisconnectPeripheral peripheral: CBPeripheral, error: Error?) {
        UnityLogger.log("Disconnected from peripheral: \(peripheral.name ?? "Unknown")")
        guard let device = discoveredPeripherals.first(where: { $0.peripheral.identifier == peripheral.identifier }) else {
            UnityLogger.logError("Disconnected peripheral not found in discovered peripherals.")
            return
        }
        device.connectionState = .disconnected
        UnityDelegates.OnPeripheralDisconnected?(peripheral.identifier.uuidString)
    }
}
