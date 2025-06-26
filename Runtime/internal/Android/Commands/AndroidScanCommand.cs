using System;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityBLE.Android
{
    internal class AndroidScanCommand
    {
        public readonly AndroidJavaClass NativePlugin;

        private readonly AndroidNativeMessageParser _messageParser;

        private readonly TaskCompletionSource<bool> _completionSource = new();

        private readonly BleScanEvents _events;

        public AndroidScanCommand(BleScanEvents events)
        {
            NativePlugin = new AndroidJavaClass("unityble.BlePlugin");
            _messageParser = new AndroidNativeMessageParser();
            BleNativePluginEventDispatcher.Instance.DeviceDiscovered += OnDeviceDiscovered;
            BleNativePluginEventDispatcher.Instance.ScanCompleted += OnScanCompleted;
            _events = events ?? throw new ArgumentNullException(nameof(events));
        }

        private void OnScanCompleted()
        {
            _completionSource.TrySetResult(true);
        }

        public void Execute(TimeSpan duration)
        {
            if (duration <= TimeSpan.Zero)
            {
                Debug.LogError("Scan duration must be greater than zero.");
                throw new ArgumentException("Scan duration must be greater than zero.", nameof(duration));
            }

            var pluginInstance = NativePlugin.CallStatic<AndroidJavaObject>("getInstance");
            if (pluginInstance == null)
            {
                throw new InvalidOperationException("Failed to get instance of BlePlugin.");
            }
            if (!pluginInstance.Call<bool>("startScan", (long)duration.TotalMilliseconds))
            {
                throw new StartScanFailed();
            }
        }

        private void OnDeviceDiscovered(string deviceJson)
        {
            var device = _messageParser.ParseDeviceData(deviceJson);
            _events._deviceDiscovered?.Invoke(device);
        }

        public void Execute(TimeSpan duration, ScanFilter filter)
        {
            if (duration <= TimeSpan.Zero)
            {
                throw new ArgumentException("Scan duration must be greater than zero.", nameof(duration));
            }

            var pluginInstance = NativePlugin.CallStatic<AndroidJavaObject>("getInstance");
            if (pluginInstance == null)
            {
                throw new InvalidOperationException("Failed to get instance of BlePlugin.");
            }
            if (!pluginInstance.Call<bool>("startScan", (long)duration.TotalMilliseconds, filter.ServiceUuids, filter.Name))
            {
                throw new StartScanFailed();
            }
        }
    }
}