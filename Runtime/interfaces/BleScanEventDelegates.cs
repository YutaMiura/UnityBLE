namespace UnityBLE
{
    public static class BleScanEventDelegates
    {
        public delegate void DeviceDiscoveredDelegate(IBlePeripheral device);
        public static event DeviceDiscoveredDelegate OnDeviceDiscovered;

        public delegate void ScanCompletedDelegate();

        public delegate void OnBLEStatusChangedDelegate(BleStatus status);

        public static event OnBLEStatusChangedDelegate BLEStatusChanged;

        public delegate void OnFoundDevicesClearedDelegate();

        public static event OnFoundDevicesClearedDelegate OnFoundDevicesCleared;

        internal static void InvokeDeviceDiscovered(IBlePeripheral device)
        {
            OnDeviceDiscovered?.Invoke(device);
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