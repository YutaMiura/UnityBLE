using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityBLE.Android
{
    internal class AndroidGetCharacteristicsCommand
    {
        private readonly AndroidJavaClass _nativePlugin;
        private readonly TaskCompletionSource<IReadOnlyList<IBleCharacteristic>> _completionSource;

        public AndroidGetCharacteristicsCommand()
        {
            _nativePlugin = new AndroidJavaClass("unityble.BlePlugin");
            _completionSource = new TaskCompletionSource<IReadOnlyList<IBleCharacteristic>>();
        }

        public async Task<IReadOnlyList<IBleCharacteristic>> ExecuteAsync(IBleService service, CancellationToken cancellationToken = default)
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service), "Service cannot be null");
            }

            if (string.IsNullOrEmpty(service.Uuid))
            {
                throw new ArgumentException("Service UUID cannot be null or empty", nameof(service));
            }

            if (string.IsNullOrEmpty(service.DeviceAddress))
            {
                throw new ArgumentException("Device address cannot be null or empty", nameof(service));
            }

            cancellationToken.ThrowIfCancellationRequested();

            Debug.Log($"[Android BLE] Getting characteristics for service {service.Uuid} on device {service.DeviceAddress}");

            try
            {
                using var pluginInstance = _nativePlugin.CallStatic<AndroidJavaObject>("getInstance");
                if (pluginInstance == null)
                {
                    throw new InvalidOperationException("BLE plugin not initialized");
                }

                // Create callback for characteristic discovery
                var callbackProxy = new CharacteristicsDiscoveryCallback(_completionSource, service);

                // Start characteristic discovery
                bool discoveryStarted = pluginInstance.Call<bool>("getCharacteristics", service.DeviceAddress, service.Uuid, callbackProxy);

                if (!discoveryStarted)
                {
                    throw new InvalidOperationException($"Failed to start characteristic discovery for service {service.Uuid}");
                }

                // Wait for result with timeout
                using var timeoutTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutTokenSource.CancelAfter(10000); // 10 second timeout

                var result = await _completionSource.Task.ConfigureAwait(false);

                Debug.Log($"[Android BLE] Successfully discovered {result.Count} characteristics for service {service.Uuid}");
                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Android BLE] Error discovering characteristics for service {service.Uuid}: {ex.Message}");
                throw;
            }
        }

        public void Dispose()
        {
            _nativePlugin?.Dispose();
        }
    }

    internal class CharacteristicsDiscoveryCallback : AndroidJavaProxy
    {
        private readonly TaskCompletionSource<IReadOnlyList<IBleCharacteristic>> _taskSource;
        private readonly IBleService _service;

        public CharacteristicsDiscoveryCallback(TaskCompletionSource<IReadOnlyList<IBleCharacteristic>> taskSource, IBleService service) 
            : base("unityble.CharacteristicsDiscoveryCallback")
        {
            _taskSource = taskSource;
            _service = service;
        }

        void onCharacteristicsDiscovered(string characteristicsJson)
        {
            try
            {
                if (string.IsNullOrEmpty(characteristicsJson))
                {
                    _taskSource.TrySetResult(new List<IBleCharacteristic>());
                    return;
                }

                // Parse characteristics JSON
                var characteristics = new List<IBleCharacteristic>();
                var characteristicData = JsonUtility.FromJson<CharacteristicsResponse>(characteristicsJson);

                if (characteristicData?.characteristics != null)
                {
                    foreach (var charInfo in characteristicData.characteristics)
                    {
                        if (!string.IsNullOrEmpty(charInfo.uuid))
                        {
                            // Convert characteristic capabilities to CharacteristicProperties
                            var properties = CharacteristicProperties.None;
                            if (charInfo.canRead) properties |= CharacteristicProperties.Read;
                            if (charInfo.canWrite) properties |= CharacteristicProperties.Write;
                            if (charInfo.canNotify) properties |= CharacteristicProperties.Notify;

                            characteristics.Add(new AndroidBleCharacteristic(charInfo.uuid, _service, properties));
                        }
                    }
                }

                _taskSource.TrySetResult(characteristics);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Android BLE] Error parsing characteristics: {ex.Message}");
                _taskSource.TrySetException(ex);
            }
        }

        void onError(string errorMessage)
        {
            Debug.LogError($"[Android BLE] Characteristic discovery error: {errorMessage}");
            _taskSource.TrySetException(new InvalidOperationException($"Characteristic discovery failed: {errorMessage}"));
        }
    }

    [System.Serializable]
    internal class CharacteristicsResponse
    {
        public CharacteristicInfo[] characteristics;
    }

    [System.Serializable]
    internal class CharacteristicInfo
    {
        public string uuid;
        public string name;
        public bool canRead;
        public bool canWrite;
        public bool canNotify;
    }
}