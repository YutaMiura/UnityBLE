using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityBLE
{
    /// <summary>
    /// Android implementation of IBleDevice.
    /// </summary>
    public class AndroidBleDevice : IBleDevice
    {
        private readonly string _name;
        private readonly string _address;
        public string Name => _name;
        public string Address => _address;

        public AndroidBleDevice(string name, string address)
        {
            _name = name;
            _address = address;
        }

        public Task ConnectAsync()
        {
            // TODO: Implement Android-specific connection logic
            return Task.CompletedTask;
        }

        public Task DisconnectAsync()
        {
            // TODO: Implement Android-specific disconnection logic
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<IBleService>> GetServicesAsync()
        {
            // TODO: Implement Android-specific service retrieval logic
            return Task.FromResult<IReadOnlyList<IBleService>>(new List<IBleService>());
        }
    }
}