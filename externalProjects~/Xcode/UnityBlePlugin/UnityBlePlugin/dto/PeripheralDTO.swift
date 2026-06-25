import Foundation
import CoreBluetooth

class PeripheralDTO: Codable {

    let uuid: String
    let name: String
    let rssi: Int
    let services: [ServiceDTO]?

    // Base64-encoded raw bytes from kCBAdvertisementDataManufacturerDataKey.
    // The payload includes the 2-byte company ID prefix as advertised.
    let manufacturerData: String?

    // Service UUID (uppercase) -> Base64-encoded payload from
    // kCBAdvertisementDataServiceDataKey.
    let serviceData: [String: String]?

    init(peripheral: CBPeripheral, rssi: NSNumber, services: [CBService]?, advertisementData: [String: Any]? = nil) {
        self.uuid = peripheral.identifier.uuidString
        self.name = peripheral.name ?? "Unknown"
        self.rssi = rssi.intValue
        self.services = services?.compactMap { ServiceDTO(service: $0) }

        if let advertisementData = advertisementData {
            if let msd = advertisementData[CBAdvertisementDataManufacturerDataKey] as? Data {
                self.manufacturerData = msd.base64EncodedString()
            } else {
                self.manufacturerData = nil
            }

            if let serviceDataDict = advertisementData[CBAdvertisementDataServiceDataKey] as? [CBUUID: Data] {
                var dict = [String: String]()
                for (uuid, data) in serviceDataDict {
                    dict[uuid.uuidString] = data.base64EncodedString()
                }
                self.serviceData = dict.isEmpty ? nil : dict
            } else {
                self.serviceData = nil
            }
        } else {
            self.manufacturerData = nil
            self.serviceData = nil
        }
    }

    enum CodingKeys: String, CodingKey {
        case uuid
        case name
        case rssi
        case services
        case manufacturerData
        case serviceData
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
