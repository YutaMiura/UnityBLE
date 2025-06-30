using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityBLE.Android
{
    internal class AndroidReadCharacteristicCommand
    {
        private readonly string _deviceAddress;
        private readonly string _serviceUuid;
        private readonly string _characteristicUuid;
        private readonly AndroidJavaClass _nativePlugin;
        private readonly TaskCompletionSource<byte[]> _completionSource;

        public AndroidReadCharacteristicCommand(string deviceAddress, string serviceUuid, string characteristicUuid)
        {
            _deviceAddress = deviceAddress ?? throw new ArgumentNullException(nameof(deviceAddress));
            _serviceUuid = serviceUuid ?? throw new ArgumentNullException(nameof(serviceUuid));
            _characteristicUuid = characteristicUuid ?? throw new ArgumentNullException(nameof(characteristicUuid));
            _nativePlugin = new AndroidJavaClass("unityble.BlePlugin");
            _completionSource = new TaskCompletionSource<byte[]>();
        }

        public async Task<byte[]> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            Debug.Log($"[Android BLE] Reading characteristic {_characteristicUuid}...");

            try
            {
                using var blePlugin = _nativePlugin.CallStatic<AndroidJavaObject>("getInstance");
                if (blePlugin == null)
                {
                    throw new InvalidOperationException("BLE plugin not initialized");
                }

                var callbackProxy = new CharacteristicReadCallback(_completionSource, blePlugin);

                bool readStarted = blePlugin.Call<bool>("readCharacteristic", _deviceAddress, _serviceUuid, _characteristicUuid, callbackProxy);

                if (!readStarted)
                {
                    throw new InvalidOperationException($"Failed to start reading characteristic {_characteristicUuid}");
                }

                using var timeoutTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutTokenSource.CancelAfter(5000); // 5 second timeout

                var result = await _completionSource.Task.ConfigureAwait(false);

                Debug.Log($"[Android BLE] Read {result.Length} bytes from characteristic {_characteristicUuid}");
                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Android BLE] Error reading characteristic {_characteristicUuid}: {ex.Message}");
                throw;
            }
        }

        public void Dispose()
        {
            _nativePlugin?.Dispose();
        }
    }

    internal class CharacteristicReadCallback : AndroidJavaProxy
    {
        private readonly TaskCompletionSource<byte[]> _taskSource;
        private readonly AndroidJavaObject _blePlugin;

        public CharacteristicReadCallback(TaskCompletionSource<byte[]> taskSource, AndroidJavaObject blePlugin) 
            : base("unityble.CharacteristicReadCallback")
        {
            _taskSource = taskSource;
            _blePlugin = blePlugin;
        }

        void onCharacteristicRead()
        {
            try
            {
                var result = _blePlugin.Call<string>("getLastReadResult");
                if (!string.IsNullOrEmpty(result))
                {
                    var bytes = System.Convert.FromBase64String(result);
                    _taskSource.TrySetResult(bytes);
                }
                else
                {
                    _taskSource.TrySetResult(new byte[0]);
                }
            }
            catch (Exception ex)
            {
                _taskSource.TrySetException(ex);
            }
        }

        void onError()
        {
            _taskSource.TrySetException(new InvalidOperationException("Read operation failed"));
        }
    }
}