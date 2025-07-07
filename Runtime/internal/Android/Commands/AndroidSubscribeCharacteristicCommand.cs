using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityBLE.Android
{
    internal class AndroidSubscribeCharacteristicCommand
    {
        private readonly string _deviceAddress;
        private readonly string _serviceUuid;
        private readonly string _characteristicUuid;
        private readonly AndroidJavaClass _nativePlugin;
        private readonly TaskCompletionSource<bool> _completionSource;
        private Action<byte[]> _notificationCallback;

        public AndroidSubscribeCharacteristicCommand(string deviceAddress, string serviceUuid, string characteristicUuid)
        {
            _deviceAddress = deviceAddress ?? throw new ArgumentNullException(nameof(deviceAddress));
            _serviceUuid = serviceUuid ?? throw new ArgumentNullException(nameof(serviceUuid));
            _characteristicUuid = characteristicUuid ?? throw new ArgumentNullException(nameof(characteristicUuid));
            _nativePlugin = new AndroidJavaClass("unityble.BlePlugin");
            _completionSource = new TaskCompletionSource<bool>();
        }

        public async Task ExecuteAsync(Action<byte[]> onValueChanged, CancellationToken cancellationToken = default)
        {
            Debug.Log($"[Android BLE] Subscribing to notifications for characteristic {_characteristicUuid}");

            try
            {
                using var blePlugin = _nativePlugin.CallStatic<AndroidJavaObject>("getInstance");
                if (blePlugin == null)
                {
                    throw new InvalidOperationException("BLE plugin not initialized");
                }

                _notificationCallback = onValueChanged;
                var callbackProxy = new CharacteristicNotificationCallback(_completionSource, this);

                bool subscribeStarted = blePlugin.Call<bool>("subscribeCharacteristic", _deviceAddress, _serviceUuid, _characteristicUuid, callbackProxy);

                if (!subscribeStarted)
                {
                    throw new InvalidOperationException($"Failed to start subscription for characteristic {_characteristicUuid}");
                }

                using var timeoutTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutTokenSource.CancelAfter(5000); // 5 second timeout

                await _completionSource.Task.ConfigureAwait(false);

                Debug.Log($"[Android BLE] Successfully subscribed to characteristic {_characteristicUuid}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Android BLE] Error subscribing to characteristic {_characteristicUuid}: {ex.Message}");
                throw;
            }
        }

        internal void HandleNotification(byte[] data)
        {
            _notificationCallback?.Invoke(data);
        }

        public void Dispose()
        {
            _nativePlugin?.Dispose();
        }
    }

    internal class AndroidUnsubscribeCharacteristicCommand
    {
        private readonly string _deviceAddress;
        private readonly string _serviceUuid;
        private readonly string _characteristicUuid;
        private readonly AndroidJavaClass _nativePlugin;
        private readonly TaskCompletionSource<bool> _completionSource;

        public AndroidUnsubscribeCharacteristicCommand(string deviceAddress, string serviceUuid, string characteristicUuid)
        {
            _deviceAddress = deviceAddress ?? throw new ArgumentNullException(nameof(deviceAddress));
            _serviceUuid = serviceUuid ?? throw new ArgumentNullException(nameof(serviceUuid));
            _characteristicUuid = characteristicUuid ?? throw new ArgumentNullException(nameof(characteristicUuid));
            _nativePlugin = new AndroidJavaClass("unityble.BlePlugin");
            _completionSource = new TaskCompletionSource<bool>();
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            Debug.Log($"[Android BLE] Unsubscribing from notifications for characteristic {_characteristicUuid}");

            try
            {
                using var blePlugin = _nativePlugin.CallStatic<AndroidJavaObject>("getInstance");
                if (blePlugin == null)
                {
                    throw new InvalidOperationException("BLE plugin not initialized");
                }

                var callbackProxy = new CharacteristicUnsubscribeCallback(_completionSource);

                bool unsubscribeStarted = blePlugin.Call<bool>("unsubscribeCharacteristic", _deviceAddress, _serviceUuid, _characteristicUuid, callbackProxy);

                if (!unsubscribeStarted)
                {
                    throw new InvalidOperationException($"Failed to start unsubscription for characteristic {_characteristicUuid}");
                }

                using var timeoutTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutTokenSource.CancelAfter(5000); // 5 second timeout

                await _completionSource.Task.ConfigureAwait(false);

                Debug.Log($"[Android BLE] Successfully unsubscribed from characteristic {_characteristicUuid}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Android BLE] Error unsubscribing from characteristic {_characteristicUuid}: {ex.Message}");
                throw;
            }
        }

        public void Dispose()
        {
            _nativePlugin?.Dispose();
        }
    }

    internal class CharacteristicNotificationCallback : AndroidJavaProxy
    {
        private readonly TaskCompletionSource<bool> _taskSource;
        private readonly AndroidSubscribeCharacteristicCommand _command;

        public CharacteristicNotificationCallback(TaskCompletionSource<bool> taskSource, AndroidSubscribeCharacteristicCommand command)
            : base("unityble.CharacteristicNotificationCallback")
        {
            _taskSource = taskSource;
            _command = command;
        }

        void onNotificationEnabled()
        {
            _taskSource.TrySetResult(true);
        }

        void onCharacteristicChanged(string dataBase64)
        {
            try
            {
                if (!string.IsNullOrEmpty(dataBase64))
                {
                    var data = System.Convert.FromBase64String(dataBase64);
                    _command.HandleNotification(data);
                    Debug.Log($"[Android BLE] Received notification: {data.Length} bytes");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Android BLE] Error processing notification: {ex.Message}");
            }
        }

        void onError()
        {
            _taskSource.TrySetException(new InvalidOperationException("Subscription failed"));
        }
    }

    internal class CharacteristicUnsubscribeCallback : AndroidJavaProxy
    {
        private readonly TaskCompletionSource<bool> _taskSource;

        public CharacteristicUnsubscribeCallback(TaskCompletionSource<bool> taskSource)
            : base("unityble.CharacteristicUnsubscribeCallback")
        {
            _taskSource = taskSource;
        }

        void onNotificationDisabled()
        {
            _taskSource.TrySetResult(true);
        }

        void onError()
        {
            _taskSource.TrySetException(new InvalidOperationException("Unsubscription failed"));
        }
    }
}