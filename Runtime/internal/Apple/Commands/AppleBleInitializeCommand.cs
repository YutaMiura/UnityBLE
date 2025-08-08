using System;
using System.Threading.Tasks;
using UnityBLE;
using UnityEngine;

namespace UnityBle.macOS
{
    public class AppleBleInitializeCommand
    {
        readonly TaskCompletionSource<bool> _tcs = new();
        public Task ExecuteAsync()
        {
            if (AppleBleNativePlugin.IsInitialized)
            {
                Debug.Log("[AppleBleNativePlugin] Already initialized");
                return Task.CompletedTask;
            }
            BleScanEventDelegates.BLEStatusChanged += OnBLEStatusChanged;
            AppleBleNativePlugin.Initialize();
            return _tcs.Task;
        }

        private void OnBLEStatusChanged(BleStatus status)
        {
            Debug.Log($"BLE Status Changed: {status}");
            try
            {
                if (status == BleStatus.PoweredOn)
                {
                    _tcs.TrySetResult(true);
                }
                else if (status == BleStatus.UnAuthorized)
                {
                    _tcs.TrySetException(new UnauthorizedAccessException("BLE is not authorized."));
                }
                else
                {
                    _tcs.TrySetException(new InvalidOperationException($"BLE Status is not PoweredOn: {status}"));
                }
            }
            finally
            {
                BleScanEventDelegates.BLEStatusChanged -= OnBLEStatusChanged;
            }
        }
    }
}