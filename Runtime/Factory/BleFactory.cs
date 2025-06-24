namespace UnityBLE
{
        /// <summary>
        /// Factory class for creating platform-specific BLE communicators.
        /// </summary>
        public static class BleCommunicatorFactory
        {
                /// <summary>
                /// Creates a platform-specific BLE communicator instance.
                /// </summary>
                /// <returns>Platform-specific implementation of IBLECommunicator</returns>
                public static IBLECommunicator Create()
                {
#if UNITY_ANDROID && !UNITY_EDITOR
                        return new AndroidBleCommunicator();
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