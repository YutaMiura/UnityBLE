namespace UnityBLE
{
    public static class BleScanEventDelegates
    {
        public delegate void DeviceDiscoveredDelegate(IBlePeripheral device);
        public static event DeviceDiscoveredDelegate OnDeviceDiscovered;

        // Fired when an already-discovered peripheral's advertisement payload
        // is meaningfully updated (e.g. ManufacturerData filled in by SCAN_RSP
        // on iOS, or any change to advertising data on Android). The
        // IBlePeripheral instance is the SAME reference as in the original
        // OnDeviceDiscovered; its mutable properties are updated in place.
        // Subscribers should re-read the peripheral's properties.
        public delegate void PeripheralUpdatedDelegate(IBlePeripheral device);
        public static event PeripheralUpdatedDelegate OnPeripheralUpdated;

        public delegate void ScanCompletedDelegate();

        public delegate void OnBLEStatusChangedDelegate(BleStatus status);

        public static event OnBLEStatusChangedDelegate BLEStatusChanged;

        public delegate void OnFoundDevicesClearedDelegate();

        public static event OnFoundDevicesClearedDelegate OnFoundDevicesCleared;

        internal static void InvokeDeviceDiscovered(IBlePeripheral device)
        {
            OnDeviceDiscovered?.Invoke(device);
        }

        internal static void InvokePeripheralUpdated(IBlePeripheral device)
        {
            OnPeripheralUpdated?.Invoke(device);
        }

        internal static void InvokeBLEStatusChanged(BleStatus status)
        {
            BLEStatusChanged?.Invoke(status);
        }

        internal static void InvokeFoundDevicesCleared()
        {
            OnFoundDevicesCleared?.Invoke();
        }
    }
}