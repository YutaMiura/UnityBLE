import Foundation

struct BleErrorDTO : Codable {
    let code: Int
    let message: String

    init(code: Int, message: String) {
        self.code = code
        self.message = message
    }

    static func PowerOff() -> BleErrorDTO {
        return BleErrorDTO(code: 1, message: "Bluetooth is powered off.")
    }

    static func Unauthorized() -> BleErrorDTO {
        return BleErrorDTO(code: 2, message: "Bluetooth is unauthorized.")
    }

    static func Unsupported() -> BleErrorDTO {
        return BleErrorDTO(code: 3, message: "Bluetooth is unsupported.")
    }

    static func Unknown() -> BleErrorDTO {
        return BleErrorDTO(code: 4, message: "Bluetooth state is unknown.")
    }

    static func ConnectionFailed(error: String) -> BleErrorDTO {
        return BleErrorDTO(code: 5, message: error)
    }

    static func InvalidOperation(error: String) -> BleErrorDTO {
        return BleErrorDTO(code: 6, message: error)
    }

    func notifyToUnity() {
        print("BLE Error \(code): \(message)")
        if let jsonData = try? JSONEncoder().encode(self) {
            UnityDelegates.OnBleErrorDetected?(String(data: jsonData, encoding: .utf8) ?? "")
        }
    }
}