using System;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityBLE.windows
{
    public class WindowsBleInitializeCommand
    {
        private readonly TaskCompletionSource<bool> _tcs = new();

        public Task ExecuteAsync()
        {
            if (WindowsBleNativePlugin.IsInitialized)
            {
                Debug.Log("[WindowsBleNativePlugin] Already initialized");
                return Task.CompletedTask;
            }
            BleScanEventDelegates.BLEStatusChanged += OnBLEStatusChanged;
            WindowsBleNativePlugin.Initialize();

            // When the radio is already powered on, the status callback may not fire.
            // Probe the current state and complete immediately if possible.
            if (TryCompleteIfPoweredOn())
            {
                BleScanEventDelegates.BLEStatusChanged -= OnBLEStatusChanged;
                return Task.CompletedTask;
            }

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

        private bool TryCompleteIfPoweredOn()
        {
            try
            {
                var state = WindowsBleNativePlugin.GetBleStatus();
                if (state == BleStatus.PoweredOn)
                {
                    _tcs.TrySetResult(true);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[WindowsBleNativePlugin] GetBleStatus failed: {ex.Message}");
                throw;
            }
            return false;
        }
    }
}
