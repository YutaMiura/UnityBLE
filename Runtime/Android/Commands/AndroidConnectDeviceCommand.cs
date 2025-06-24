using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityBLE
{
    public class AndroidConnectDeviceCommand : IConnectDeviceCommand
    {
        public readonly AndroidJavaClass NativePlugin;
        private readonly TaskCompletionSource<IBleDevice> _connectionCompletionSource;
        private readonly IBleNativeMessageParser _messageParser = new AndroidNativeMessageParser();

        public AndroidConnectDeviceCommand()
        {
            NativePlugin = new AndroidJavaClass("unityble.BlePlugin");
            _connectionCompletionSource = new TaskCompletionSource<IBleDevice>();
            BleNativePluginEventDispatcher.Instance.DeviceConnected += OnConnected;
        }

        private void OnConnected(string msg)
        {
            Debug.Log($"Device connected: {msg}");
            _connectionCompletionSource.SetResult(_messageParser.ParseDeviceData(msg));
        }

        public Task<IBleDevice> ExecuteAsync(string address, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(address))
            {
                throw new System.ArgumentException("Device address is not set.", nameof(address));
            }

            cancellationToken.ThrowIfCancellationRequested();

            cancellationToken.Register(() =>
            {
                Debug.Log($"Connection to device {address} was cancelled.");
                _connectionCompletionSource.SetCanceled();
            });

            var pluginInstance = NativePlugin.CallStatic<AndroidJavaObject>("getInstance");
            if (!pluginInstance.Call<bool>("connectToDevice", address))
            {
                _connectionCompletionSource.SetException(new ConnectionFailed(address));
            }
            return _connectionCompletionSource.Task;
        }
    }
}