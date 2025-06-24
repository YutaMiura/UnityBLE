using System;

namespace UnityBLE
{
    public static class BlePermissionServiceFactory
    {
        /// <summary>
        /// Creates an instance of the BLE permission service.
        /// </summary>
        /// <returns>An instance of IBlePermissionService.</returns>
        public static IBlePermissionService Create()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return new AndroidBlePermissionService();
#elif UNITY_IOS && !UNITY_EDITOR
            throw new NotImplementedException("BlePermissionService is not implemented for iOS yet.");
#elif UNITY_EDITOR_OSX
            throw new NotImplementedException("BlePermissionService is not implemented for macOS editor yet.");
#elif UNITY_EDITOR_64
            throw new NotImplementedException("BlePermissionService is not implemented for 64-bit editor yet.");
#endif
        }
    }
}