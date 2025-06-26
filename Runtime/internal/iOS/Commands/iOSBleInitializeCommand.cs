using System;
using System.Threading.Tasks;
using UnityBLE.iOS;

namespace UnityBLE.iOS
{
    public class iOSBleInitializeCommand
    {

        public async Task ExecuteAsync()
        {
            if (iOSBleNativePlugin.IsBluetoothReady())
            {
                return;
            }

            iOSBleNativePlugin.Initialize();
            // if (!iOSBleNativePlugin.WaitForBluetoothReady(5000))
            // {
            //     throw new TimeoutException("Initialize timed out.");
            // }
        }
    }
}