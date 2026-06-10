#if UNITY_EDITOR_OSX || UNITY_IOS
using UnityBLE.apple;
#elif !UNITY_EDITOR && UNITY_ANDROID
using UnityBLE.Android;
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
using UnityBLE.windows;
#else
using System;
#endif

namespace UnityBLE
{
    public class BleManager
    {
        public static BleManager Instance => _instance ??= new BleManager();
        private static BleManager _instance;

        public readonly IBleScanner Scanner;

        private BleManager()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            Scanner = new AndroidBleScanner();
#elif UNITY_EDITOR_OSX || UNITY_IOS
            Scanner = new AppleBleScanner();
#elif UNITY_EDITOR_WIN || (UNITY_STANDALONE_WIN && !UNITY_EDITOR)
            Scanner = new WindowsBleScanner();
#elif UNITY_EDITOR_64
            throw new NotSupportedException("BleManager is not supported on this Unity Editor platform.");
#else
            throw new NotSupportedException("BleManager is not supported this platform.");
#endif
        }

    }
}