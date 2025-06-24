using System;

namespace UnityBLE
{
    public class ConnectionFailed : BleError
    {
        const string ERR_CODE = "CONNECTION_FAILED";
        public ConnectionFailed(string deviceAddress)
            : base($"Failed to connect to device", ERR_CODE, $"Device address: {deviceAddress}")
        {
        }

        public ConnectionFailed(string deviceAddress, Exception innerException)
            : base($"Failed to connect to device", ERR_CODE, $"Device address: {deviceAddress}", innerException)
        {
        }
    }
}