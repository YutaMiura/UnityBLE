using UnityBLE.macOS;

namespace UnityBLE
{
    public class BleManager
    {
        public static BleManager Instance => _instance ??= new BleManager();
        private static BleManager _instance;

        public readonly IBleScanner Scanner;

        public IBleDevice ConnectedDevice { get; private set; }

        internal void SaveConnectedDevice(IBleDevice device)
        {
            ConnectedDevice = device;
        }

        internal void DisconnectDevice()
        {
            ConnectedDevice = null;
        }

        private BleManager()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            Scanner = new AndroidBleScanner();
#elif UNITY_IOS && !UNITY_EDITOR
            throw new NotSupportedException("BleManager is not supported in iOS platform.");
#elif UNITY_EDITOR_OSX
            Scanner = new MacOSBleScanner();
#elif UNITY_EDITOR_64
            throw new NotSupportedException("BleManager is not supported in Windows Unity Editor.");
#else
            throw new NotSupportedException("BleManager is not supported this platform.");
#endif
        }

    }
}