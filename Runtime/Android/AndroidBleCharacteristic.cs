using System;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityBLE
{
    /// <summary>
    /// Android implementation of IBleCharacteristic.
    /// </summary>
    public class AndroidBleCharacteristic : IBleCharacteristic
    {
        private readonly string _uuid;
        private readonly IBleService _service;
        public string Uuid => _uuid;
        public IBleService Service => _service;

        public AndroidBleCharacteristic(string uuid, IBleService service)
        {
            _uuid = uuid;
            _service = service;
        }

        public Task<byte[]> ReadAsync()
        {
            // TODO: Implement Android-specific read logic
            return Task.FromResult(new byte[0]);
        }

        public Task WriteAsync(byte[] data, bool withResponse)
        {
            // TODO: Implement Android-specific write logic
            return Task.CompletedTask;
        }

        public Task SubscribeAsync(Action<byte[]> onValueChanged)
        {
            // TODO: Implement Android-specific subscription logic
            return Task.CompletedTask;
        }

        public Task UnsubscribeAsync()
        {
            // TODO: Implement Android-specific unsubscription logic
            return Task.CompletedTask;
        }
    }
}