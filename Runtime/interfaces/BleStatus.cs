namespace UnityBLE
{
    /// <summary>
    /// Enumeration representing the status of the Bluetooth Low Energy (BLE) system.
    /// This is according to the CBManagerState in CoreBluetooth.
    /// https://developer.apple.com/documentation/corebluetooth/cbmanagerstate
    /// </summary>
    public enum BleStatus
    {
        Unknown,
        Resetting,
        UnSupported,
        UnAuthorized,
        PoweredOff,
        PoweredOn,
    }
}