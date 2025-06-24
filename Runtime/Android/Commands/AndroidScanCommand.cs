using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityBLE
{
    public class AndroidScanCommand : IScanCommand
    {
        public readonly AndroidJavaClass NativePlugin;

        private readonly IBleNativeMessageParser _messageParser;

        public AndroidScanCommand()
        {
            NativePlugin = new AndroidJavaClass("unityble.BlePlugin");
            _messageParser = new AndroidNativeMessageParser();
            BleNativePluginEventDispatcher.Instance.DeviceDiscovered += OnDeviceDiscovered;
            BleNativePluginEventDispatcher.Instance.ScanCompleted += OnScanCompleted;
        }

        private void OnScanCompleted()
        {
            BleScanner.Instance.Events._scanCompleted?.Invoke();
        }

        public Task ExecuteAsync(TimeSpan duration, CancellationToken cancellationToken = default)
        {
            if (duration <= TimeSpan.Zero)
            {
                Debug.LogError("Scan duration must be greater than zero.");
                return Task.FromException(new ArgumentException("Scan duration must be greater than zero."));
            }

            cancellationToken.ThrowIfCancellationRequested();

            var completionSource = new TaskCompletionSource<bool>();

            var pluginInstance = NativePlugin.CallStatic<AndroidJavaObject>("getInstance");
            if (!pluginInstance.Call<bool>("startScan", (long)duration.TotalMilliseconds))
            {
                Debug.LogError("Failed to start scan.");
                completionSource.TrySetException(new InvalidOperationException("Failed to start scan."));
                return completionSource.Task;
            }

            cancellationToken.Register(async () =>
            {
                if (await new AndroidStopScanCommand().ExecuteAsync())
                {
                    completionSource.TrySetCanceled();
                }
            });

            return completionSource.Task;
        }

        private void OnDeviceDiscovered(string deviceJson)
        {
            var device = _messageParser.ParseDeviceData(deviceJson);
            BleScanner.Instance.Events._deviceDiscovered?.Invoke(device);
        }

        public Task ExecuteAsync(TimeSpan duration, ScanFilter filter, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}