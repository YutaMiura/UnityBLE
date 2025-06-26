using System;
using System.Threading.Tasks;
using UnityBLE;

namespace UnityBle.macOS
{
    public class MacOSBleInitializeCommand
    {

        public async Task ExecuteAsync()
        {
            if (MacOSBleNativePlugin.IsBluetoothReady())
            {
                return;
            }

            MacOSBleNativePlugin.Initialize();
        }
    }
}