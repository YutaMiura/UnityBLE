import Foundation
import CoreBluetooth

class CharacteristicDTO: Codable {
    let peripheralUUID: String
    let serviceUUID: String
    let uuid: String
    let value: String?
    let isReadable: Bool
    let isWritable: Bool
    let isNotifiable: Bool

    init(characteristic: CBCharacteristic) {
        self.peripheralUUID = characteristic.service?.peripheral?.identifier.uuidString ?? ""
        self.serviceUUID = characteristic.service?.uuid.uuidString ?? ""
        self.uuid = characteristic.uuid.uuidString
        self.isReadable = characteristic.properties.contains(.read)
        self.isWritable = characteristic.properties.contains(.write)
        self.isNotifiable = characteristic.properties.contains(.notify)
        self.value = characteristic.value?.base64EncodedString()
    }



    enum CodingKeys: String, CodingKey {
        case peripheralUUID
        case serviceUUID
        case uuid
        case isReadable
        case isWritable
        case isNotifiable
        case value
    }

    func toJson() -> String? {
        let encoder = JSONEncoder()
        do {
            let jsonData = try encoder.encode(self)
            return String(data: jsonData, encoding: .utf8)
        } catch {
            print("Error encoding CharacteristicDTO to JSON: \(error)")
            return nil
        }
    }
}