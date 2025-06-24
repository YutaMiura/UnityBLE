using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityBLE
{
    public class AndroidStopScanCommand : IStopScanCommand
    {
        public readonly AndroidJavaClass NativePlugin;

        public AndroidStopScanCommand()
        {
            NativePlugin = new AndroidJavaClass("unityble.BlePlugin");
        }

        public Task<bool> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var pluginInstance = NativePlugin.CallStatic<AndroidJavaObject>("getInstance");
            return Task.FromResult(pluginInstance.Call<bool>("stopScan"));
        }
    }
}