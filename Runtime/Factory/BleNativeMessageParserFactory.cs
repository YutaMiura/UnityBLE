namespace UnityBLE
{
    internal static class BleNativeMessageParserFactory
    {
        public static IBleNativeMessageParser Create()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return new AndroidNativeMessageParser();
#elif UNITY_IOS && !UNITY_EDITOR
            // TODO: Implement iOS BLE communicator
            throw new System.NotImplementedException("iOS BLE communicator not yet implemented");
#elif UNITY_EDITOR
            // TODO: Implement Editor mock/simulator BLE communicator
            throw new System.NotImplementedException("Editor BLE communicator not yet implemented");
#else
            throw new System.PlatformNotSupportedException($"BLE is not supported on this platform: {Application.platform}");
#endif
        }
    }
}