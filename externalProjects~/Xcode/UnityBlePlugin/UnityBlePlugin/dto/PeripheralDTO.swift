import Foundation
import CoreBluetooth

class PeripheralDTO: Codable {

    let uuid: String
    let name: String
    let rssi: Int
    let services: [ServiceDTO]?

    init(peripheral: CBPeripheral, rssi: NSNumber, services: [CBService]?) {
        self.uuid = peripheral.identifier.uuidString
        self.name = peripheral.name ?? "Unknown"
        self.rssi = rssi.intValue
        self.services = services?.compactMap { ServiceDTO(service: $0) }
    }

    enum CodingKeys: String, CodingKey {
        case uuid
        case name
        case rssi
        case services
    }

    func toJson() -> String? {
        let encoder = JSONEncoder()
        do {
            let jsonData = try encoder.encode(self)
            return String(data: jsonData, encoding: .utf8)
        } catch {
            print("Error encoding PeripheralDTO to JSON: \(error)")
            return nil
        }
    }
}