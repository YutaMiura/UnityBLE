namespace UnityBLE.Android
{
    internal partial class AndroidBleNativePlugin
    {
        private const string CLASS_BLE_MANAGER = "jp.yuta.miura.unityble.BleManager";
        private const string METHOD_NAME_START_SCAN = "startBleScan";
        private const string METHOD_NAME_STOP_SCAN = "stopScan";

        private const string METHOD_NAME_CONNECT = "connect";

        private const string METHOD_NAME_DISCONNECT = "disconnect";

        private const string METHOD_NAME_DISCOVERY_SERVICES = "discoveryServices";

        private const string METHOD_NAME_READ = "read";
        private const string METHOD_NAME_WRITE = "write";
        private const string METHOD_NAME_SUBSCRIBE = "subscribe";
        private const string METHOD_NAME_UNSUBSCRIBE = "unsubscribe";
    }
}