import Foundation
import CoreBluetooth

class ServiceDTO: Codable {
    let description: String
    let peripheralUUID: String
    let uuid: String
    let characteristics: [CharacteristicDTO]

    init(service: CBService) {
        self.description = service.description
        self.peripheralUUID = service.peripheral?.identifier.uuidString ?? ""
        self.uuid = service.uuid.uuidString
        self.characteristics = service.characteristics?.map { CharacteristicDTO(characteristic: $0) } ?? []
    }

    enum CodingKeys: String, CodingKey {
        case description
        case peripheralUUID
        case uuid
        case characteristics
    }

    func toJson() -> String? {
        let encoder = JSONEncoder()
        do {
            let jsonData = try encoder.encode(self)
            return String(data: jsonData, encoding: .utf8)
        } catch {
            print("Error encoding ServiceDTO to JSON: \(error)")
            return nil
        }
    }
}