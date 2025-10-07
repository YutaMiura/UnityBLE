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

            // macOS Editor: when already PoweredOn, the status callback may not fire.
            // Probe by scan capability and complete immediately if possible.
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
            // State-only check: rely solely on native bridge state.
            // No scan start/stop probing to avoid side effects.
            try
            {
                var state = AppleBleNativePlugin.GetBleStatus();
                if (state == BleStatus.PoweredOn)
                {
                    _tcs.TrySetResult(true);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AppleBleNativePlugin] GetBleStatus failed: {ex.Message}");
                throw;
            }
            return false;
        }
    }
}
