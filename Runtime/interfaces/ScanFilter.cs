using System;

namespace UnityBLE
{
    public class ScanFilter
    {
        public string[] ServiceUuids { get; set; }
        public string Name { get; set; }

        // When true, the scanner asks the OS to deliver every advertisement
        // frame including SCAN_RSP (the second frame in BLE's two-packet
        // advertisement flow). This is required to receive Manufacturer
        // Specific Data that the peripheral places in SCAN_RSP rather than
        // ADV_IND. Maps to:
        //   iOS / macOS: CBCentralManagerScanOptionAllowDuplicatesKey = true
        //   Android:     ScanSettings.callbackType = CALLBACK_TYPE_ALL_MATCHES
        //                (this is the Android default; included here for
        //                 explicit cross-platform parity)
        // Note: turning this on increases callback frequency and battery
        // consumption. Keep on only while the user actually needs scan results.
        public bool ReceiveScanResponse { get; set; }

        public ScanFilter(string[] serviceUuids = null, string name = null, bool receiveScanResponse = true)
        {
            ServiceUuids = serviceUuids ?? Array.Empty<string>();
            Name = name ?? string.Empty;
            ReceiveScanResponse = receiveScanResponse;
        }

        public static ScanFilter None => new();

        public bool NoFilter()
        {
            return ServiceUuids.Length == 0 && string.IsNullOrEmpty(Name);
        }

        public override string ToString()
        {
            return $"ScanFilter(Name: {Name}, ServiceUuids: [{string.Join(", ", ServiceUuids)}], ReceiveScanResponse: {ReceiveScanResponse})";
        }
    }
}