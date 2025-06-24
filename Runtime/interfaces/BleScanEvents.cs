using System;

namespace UnityBLE
{
    /// <summary>
    /// Main facade for Unity BLE operations.
    /// Provides a unified interface for BLE communication across platforms.
    /// </summary>
    public class BleScanEvents
    {
        internal Action<IBleDevice> _deviceDiscovered;
        internal Action _scanCompleted;
        internal Action<Exception> _scanFailed;

        /// <summary>
        /// Event triggered when a BLE device is discovered.
        /// </summary>
        public event Action<IBleDevice> DeviceDiscovered
        {
            add
            {
                _deviceDiscovered += value;
                // Ensure the event is initialized if not already
                if (_deviceDiscovered == null)
                {
                    _deviceDiscovered = value;
                }
            }
            remove
            {
                _deviceDiscovered -= value;
            }
        }
        /// <summary>
        /// Event triggered when the BLE scan is completed.
        /// </summary>
        public event Action ScanCompleted
        {
            add
            {
                _scanCompleted += value;
                // Ensure the event is initialized if not already
                if (_scanCompleted == null)
                {
                    _scanCompleted = value;
                }
            }
            remove
            {
                _scanCompleted -= value;
            }
        }
        /// <summary>
        /// Event triggered when the BLE scan fails.
        /// </summary>
        public event Action<Exception> ScanFailed
        {
            add
            {
                _scanFailed += value;
                // Ensure the event is initialized if not already
                if (_scanFailed == null)
                {
                    _scanFailed = value;
                }
            }
            remove
            {
                _scanFailed -= value;
            }
        }
    }
}