using System;

namespace UnityBLE
{
    public static class BleCommandFactory
    {
        public static IScanCommand CreateStartScanCommand()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return new StartScanCommand();
#elif UNITY_IOS && !UNITY_EDITOR
            throw new NotImplementedException("StartScanCommand is not implemented for iOS yet.");
#elif UNITY_EDITOR_OSX
            throw new NotImplementedException("StartScanCommand is not implemented for macOS editor yet.");
#elif UNITY_EDITOR_64
            throw new NotImplementedException("StartScanCommand is not implemented for Windows editor yet.");
#else
            throw new PlatformNotSupportedException($"StartScanCommand is not supported on this platform: {Environment.OSVersion.Platform}");
#endif
        }

        public static IStopScanCommand CreateStopScanCommand()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return new StopScanCommand();
#elif UNITY_IOS && !UNITY_EDITOR
            throw new NotImplementedException("StopScanCommand is not implemented for iOS yet.");
#elif UNITY_EDITOR_OSX
            throw new NotImplementedException("StopScanCommand is not implemented for macOS editor yet.");
#elif UNITY_EDITOR_64
            throw new NotImplementedException("StopScanCommand is not implemented for Windows editor yet.");
#else
            throw new PlatformNotSupportedException($"StopScanCommand is not supported on this platform: {Environment.OSVersion.Platform}");
#endif
        }

        public static IConnectDeviceCommand CreateConnectDeviceCommand()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return new StartScanCommand();
#elif UNITY_IOS && !UNITY_EDITOR
            throw new NotImplementedException("StartScanCommand is not implemented for iOS yet.");
#elif UNITY_EDITOR_OSX
            throw new NotImplementedException("StartScanCommand is not implemented for macOS editor yet.");
#elif UNITY_EDITOR_64
            throw new NotImplementedException("StartScanCommand is not implemented for Windows editor yet.");
#else
            throw new PlatformNotSupportedException($"StartScanCommand is not supported on this platform: {Environment.OSVersion.Platform}");
#endif
        }

        public static IDisconnectDeviceCommand CreateDisconnectDeviceCommand()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return new DisconnectDeviceCommand();
#elif UNITY_IOS && !UNITY_EDITOR
            throw new NotImplementedException("StartScanCommand is not implemented for iOS yet.");
#elif UNITY_EDITOR_OSX
            throw new NotImplementedException("StartScanCommand is not implemented for macOS editor yet.");
#elif UNITY_EDITOR_64
            throw new NotImplementedException("StartScanCommand is not implemented for Windows editor yet.");
#else
            throw new PlatformNotSupportedException($"StartScanCommand is not supported on this platform: {Environment.OSVersion.Platform}");
#endif
        }
    }
}