import Foundation

public class UnityLogger: NSObject {

    public static var OnLog: ((String) -> Void)?

    static func log(_ message: String) {
        print("UnityLogger: \(message)")
        OnLog?(message)
    }

    static func logError(_ message: String) {
        print("UnityLogger Error: \(message)")
        OnLog?("Error: \(message)")
    }

    static func logWarning(_ message: String) {
        print("UnityLogger Warning: \(message)")
        OnLog?("Warning: \(message)")
    }

    static func logInfo(_ message: String) {
        print("UnityLogger Info: \(message)")
        OnLog?("Info: \(message)")
    }

}