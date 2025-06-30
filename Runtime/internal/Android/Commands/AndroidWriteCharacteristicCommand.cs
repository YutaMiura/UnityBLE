using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityBLE.Android
{
    internal class AndroidWriteCharacteristicCommand
    {
        private readonly string _deviceAddress;
        private readonly string _serviceUuid;
        private readonly string _characteristicUuid;
        private readonly byte[] _data;
        private readonly bool _withResponse;
        private readonly AndroidJavaClass _nativePlugin;
        private readonly TaskCompletionSource<bool> _completionSource;

        public AndroidWriteCharacteristicCommand(string deviceAddress, string serviceUuid, string characteristicUuid, byte[] data, bool withResponse)
        {
            _deviceAddress = deviceAddress ?? throw new ArgumentNullException(nameof(deviceAddress));
            _serviceUuid = serviceUuid ?? throw new ArgumentNullException(nameof(serviceUuid));
            _characteristicUuid = characteristicUuid ?? throw new ArgumentNullException(nameof(characteristicUuid));
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _withResponse = withResponse;
            _nativePlugin = new AndroidJavaClass("unityble.BlePlugin");
            _completionSource = new TaskCompletionSource<bool>();
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            Debug.Log($"[Android BLE] Writing {_data.Length} bytes to characteristic {_characteristicUuid} {(_withResponse ? "with" : "without")} response...");

            try
            {
                using var blePlugin = _nativePlugin.CallStatic<AndroidJavaObject>("getInstance");
                if (blePlugin == null)
                {
                    throw new InvalidOperationException("BLE plugin not initialized");
                }

                string dataBase64 = System.Convert.ToBase64String(_data);
                var callbackProxy = new CharacteristicWriteCallback(_completionSource);

                bool writeStarted = blePlugin.Call<bool>("writeCharacteristic", _deviceAddress, _serviceUuid, _characteristicUuid, dataBase64, _withResponse, callbackProxy);

                if (!writeStarted)
                {
                    throw new InvalidOperationException($"Failed to start writing to characteristic {_characteristicUuid}");
                }

                using var timeoutTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutTokenSource.CancelAfter(5000); // 5 second timeout

                await _completionSource.Task.ConfigureAwait(false);

                Debug.Log($"[Android BLE] Successfully wrote {_data.Length} bytes to characteristic {_characteristicUuid}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Android BLE] Error writing characteristic {_characteristicUuid}: {ex.Message}");
                throw;
            }
        }

        public void Dispose()
        {
            _nativePlugin?.Dispose();
        }
    }

    internal class CharacteristicWriteCallback : AndroidJavaProxy
    {
        private readonly TaskCompletionSource<bool> _taskSource;

        public CharacteristicWriteCallback(TaskCompletionSource<bool> taskSource) 
            : base("unityble.CharacteristicWriteCallback")
        {
            _taskSource = taskSource;
        }

        void onCharacteristicWrite()
        {
            _taskSource.TrySetResult(true);
        }

        void onError()
        {
            _taskSource.TrySetException(new InvalidOperationException("Write operation failed"));
        }
    }
}