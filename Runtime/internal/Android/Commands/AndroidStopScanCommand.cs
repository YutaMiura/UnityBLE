using System;
using UnityEngine;

namespace UnityBLE.Android
{
    internal class AndroidStopScanCommand
    {
        public readonly AndroidJavaClass NativePlugin;

        public AndroidStopScanCommand()
        {
            NativePlugin = new AndroidJavaClass("unityble.BlePlugin");
        }

        public bool Execute()
        {
            var pluginInstance = NativePlugin.CallStatic<AndroidJavaObject>("getInstance");
            if (pluginInstance == null)
            {
                throw new InvalidOperationException("Failed to get instance of BlePlugin.");
            }
            return pluginInstance.Call<bool>("stopScan");
        }
    }
}