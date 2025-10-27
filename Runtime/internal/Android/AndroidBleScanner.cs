using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityBLE.Android
{
    public sealed class AndroidBleScanner : IBleScanner
    {
        private NativeFacade _facade;

        private CancellationTokenRegistration _ctsRegistration;

        public event Action<bool> OnScanningStateChanged;
        private static SynchronizationContext _unityMainThreadContext;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CaptureUnityMainThreadContext()
        {
            _unityMainThreadContext = SynchronizationContext.Current;
        }

        public Task InitializeAsync()
        {
            _facade = NativeFacade.Instance;
            return Task.CompletedTask;
        }

        public Task StartScan(BleScanEventDelegates.DeviceDiscoveredDelegate OnDeviceDiscovered, CancellationToken cancellationToken)
        {
            return StartScan(ScanFilter.None, OnDeviceDiscovered, cancellationToken);
        }

        public async Task StartScan(ScanFilter filter, BleScanEventDelegates.DeviceDiscoveredDelegate OnDeviceDiscovered, CancellationToken cancellationToken)
        {
            if (_facade == null)
            {
                Debug.LogError("AndroidBleScanner is not initialized.");
                throw new InvalidOperationException("AndroidBleScanner is not initialized.");
            }
            Debug.Log($"AndroidBleScanner.StartScan() called with filter: {filter ?? ScanFilter.None}");
            _ctsRegistration = cancellationToken.Register(() =>
            {
                if (_unityMainThreadContext != null)
                {
                    _unityMainThreadContext.Post(_ => _ = StopScan(), null);
                    return;
                }
            });
            await _facade.StartScan(
                filter,
                OnDeviceDiscovered);
            OnScanningStateChanged?.Invoke(true);
        }

        public async Task<bool> StopScan()
        {
            if (_facade == null)
            {
                Debug.LogError("AndroidBleScanner is not initialized.");
                throw new InvalidOperationException("AndroidBleScanner is not initialized.");
            }

            Debug.Log("AndroidBleScanner.StopScan() called.");
            var result = await _facade.StopScanAsync();
            if (result)
            {
                OnScanningStateChanged?.Invoke(false);
            }
            _ctsRegistration.Dispose();
            return result;
        }
    }
}