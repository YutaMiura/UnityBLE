# Unity BLE Android Plugin

A Unity plugin for Bluetooth Low Energy (BLE) communication on Android devices.

## Features

- **Event-driven scanning**: Real-time device discovery via events
- **Device connection management**: Connect/disconnect to BLE devices
- **Service discovery**: Automatic discovery of device services
- **Filtered scanning**: Scan by service UUIDs or device names
- **Permission handling**: Automatic Android permission management
- **Error handling**: Comprehensive error reporting
- **Cross-platform interface**: Consistent API across platforms

## Quick Start

### 1. Initialize the BLE Manager

```csharp
using UnityBLE;
using System;
using System.Threading;
using System.Collections.Generic;

public class BleExample : MonoBehaviour
{
    private BleManager bleManager;
    private IBleDevice targetDevice;

    async void Start()
    {
        // Get the BLE manager instance
        bleManager = BleManager.Instance;

        // Subscribe to events
        bleManager.DeviceDiscovered += OnDeviceDiscovered;
        bleManager.ScanCompleted += OnScanCompleted;
        bleManager.ScanFailed += OnScanFailed;

        // Subscribe to connection events
        bleManager.DeviceConnected += OnDeviceConnected;
        bleManager.DeviceDisconnected += OnDeviceDisconnected;
        bleManager.ConnectionFailed += OnConnectionFailed;
        bleManager.ServicesDiscovered += OnServicesDiscovered;

        try
        {
            // Initialize the BLE system
            await bleManager.InitializeAsync();
            Debug.Log("BLE initialized successfully");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to initialize BLE: {e.Message}");
        }
    }

    private void OnDeviceDiscovered(IBleDevice device)
    {
        Debug.Log($"Device found: {device.Name} ({device.Address})");

        // Cast to AndroidBleDevice to access Android-specific properties
        if (device is AndroidBleDevice androidDevice)
        {
            Debug.Log($"RSSI: {androidDevice.Rssi}, Connectable: {androidDevice.IsConnectable}");

            // Save reference to connect later
            if (device.Name.Contains("MyTargetDevice"))
            {
                targetDevice = device;
            }
        }
    }

    private void OnScanCompleted()
    {
        Debug.Log("Scan completed");

        // Connect to target device if found
        if (targetDevice != null)
        {
            ConnectToDevice();
        }
    }

    private void OnScanFailed(string error)
    {
        Debug.LogError($"Scan failed: {error}");
    }

    private void OnDeviceConnected(string deviceAddress)
    {
        Debug.Log($"Device connected: {deviceAddress}");
    }

    private void OnDeviceDisconnected(string deviceAddress)
    {
        Debug.Log($"Device disconnected: {deviceAddress}");
    }

    private void OnConnectionFailed(string error)
    {
        Debug.LogError($"Connection failed: {error}");
    }

    private void OnServicesDiscovered(string deviceAddress, IReadOnlyList<IBleService> services)
    {
        Debug.Log($"Services discovered for {deviceAddress}: {services.Count} services");

        foreach (var service in services)
        {
            Debug.Log($"Service UUID: {service.Uuid}");
        }
    }
}
```

### 2. Start Scanning

```csharp
// Basic scan for 10 seconds
bleManager.StartScan(TimeSpan.FromSeconds(10));

// Scan with cancellation
var cts = new CancellationTokenSource();
bleManager.StartScan(TimeSpan.FromSeconds(15), cts.Token);

// Cancel scan early
cts.Cancel();
```

### 3. Device Connection

```csharp
private async void ConnectToDevice()
{
    if (targetDevice != null)
    {
        try
        {
            Debug.Log($"Connecting to {targetDevice.Name}...");
            await targetDevice.ConnectAsync();
            Debug.Log("Connected successfully!");

            // Get services after connection
            var services = await targetDevice.GetServicesAsync();
            Debug.Log($"Found {services.Count} services");

            foreach (var service in services)
            {
                Debug.Log($"Service: {service.Uuid}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Connection failed: {e.Message}");
        }
    }
}

private async void DisconnectFromDevice()
{
    if (targetDevice != null)
    {
        try
        {
            await targetDevice.DisconnectAsync();
            Debug.Log("Disconnected successfully!");
        }
        catch (Exception e)
        {
            Debug.LogError($"Disconnection failed: {e.Message}");
        }
    }
}
```

### 4. Using BleManager for Connection Management

```csharp
// Connect using BleManager directly
bool success = bleManager.ConnectToDevice("AA:BB:CC:DD:EE:FF");

// Check connection status
bool isConnected = bleManager.IsDeviceConnected("AA:BB:CC:DD:EE:FF");

// Get connected device reference
AndroidBleDevice device = bleManager.GetConnectedDevice("AA:BB:CC:DD:EE:FF");

// Disconnect
bool disconnected = bleManager.DisconnectFromDevice("AA:BB:CC:DD:EE:FF");
```

### 5. Filtered Scanning

```csharp
// Scan for specific service UUIDs
bleManager.StartScanByServiceUuids(
    TimeSpan.FromSeconds(15),
    CancellationToken.None,
    "180D", "180F" // Heart Rate and Battery services
);

// Scan for specific device names
bleManager.StartScanByNames(
    TimeSpan.FromSeconds(10),
    CancellationToken.None,
    "MyDevice", "AnotherDevice"
);
```

### 6. Complete Example: Heart Rate Monitor

```csharp
public class HeartRateMonitor : MonoBehaviour
{
    private BleManager bleManager;
    private IBleDevice heartRateDevice;

    async void Start()
    {
        bleManager = BleManager.Instance;

        // Subscribe to events
        bleManager.DeviceDiscovered += OnDeviceDiscovered;
        bleManager.DeviceConnected += OnDeviceConnected;
        bleManager.ServicesDiscovered += OnServicesDiscovered;

        await bleManager.InitializeAsync();

        // Scan for Heart Rate devices
        bleManager.StartScanByServiceUuids(
            TimeSpan.FromSeconds(10),
            CancellationToken.None,
            "180D" // Heart Rate Service UUID
        );
    }

    private void OnDeviceDiscovered(IBleDevice device)
    {
        if (device.Name.Contains("Heart Rate"))
        {
            heartRateDevice = device;
            bleManager.StopScan();
        }
    }

    private async void OnDeviceConnected(string deviceAddress)
    {
        if (heartRateDevice?.Address == deviceAddress)
        {
            try
            {
                var services = await heartRateDevice.GetServicesAsync();
                Debug.Log($"Heart Rate device services: {services.Count}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error getting services: {e.Message}");
            }
        }
    }

    private void OnServicesDiscovered(string deviceAddress, IReadOnlyList<IBleService> services)
    {
        foreach (var service in services)
        {
            if (service.Uuid.ToUpper().Contains("180D"))
            {
                Debug.Log("Found Heart Rate Service!");
                // TODO: Get characteristics and subscribe to heart rate measurements
            }
        }
    }
}
```

## Event-Driven Architecture

The plugin uses an event-driven architecture that matches Android BLE's natural behavior:

**Scanning Events:**
- **DeviceDiscovered**: Fired when a new BLE device is found
- **ScanCompleted**: Fired when the scan finishes (timeout or manual stop)
- **ScanFailed**: Fired when the scan encounters an error

**Connection Events:**
- **DeviceConnected**: Fired when a device connection is established
- **DeviceDisconnected**: Fired when a device is disconnected
- **ConnectionFailed**: Fired when a connection attempt fails
- **ServicesDiscovered**: Fired when device services are discovered

This approach provides:
- **Real-time updates**: Events are reported as soon as they occur
- **Better performance**: No need to wait for operations to complete
- **Flexible handling**: Process events as they happen

## AndroidBleDevice Properties

The `AndroidBleDevice` class provides Android-specific BLE information:

- **Name**: Device name
- **Address**: MAC address
- **Rssi**: Signal strength indicator
- **IsConnectable**: Whether the device can be connected to
- **TxPower**: Transmit power level
- **AdvertisingData**: Raw advertising data (Base64 encoded)
- **IsConnected**: Current connection status

## Connection Management

The plugin provides comprehensive connection management:

```csharp
// Connect to a device
await device.ConnectAsync();

// Check connection status
bool connected = device.IsConnected;

// Get device services
var services = await device.GetServicesAsync();

// Disconnect
await device.DisconnectAsync();
```

## Error Handling

The plugin provides comprehensive error handling:

- **Connection timeouts**: Automatic handling of connection timeouts
- **Service discovery failures**: Proper error reporting for service discovery
- **Permission denial exceptions**: Clear error messages for permission issues
- **Bluetooth adapter unavailability**: Graceful handling when Bluetooth is disabled

## Permissions

The plugin automatically handles Android permissions based on the target SDK version:

- **Android 12+ (API 31+)**: `BLUETOOTH_SCAN`, `BLUETOOTH_CONNECT`
- **Android 11 and below**: `BLUETOOTH`, `BLUETOOTH_ADMIN`
- **All versions**: `ACCESS_FINE_LOCATION` (required for BLE scanning)

Permissions are requested automatically during initialization.

## AndroidManifest.xml

The plugin includes an `AndroidManifest.xml` with the necessary permissions and feature declarations:

```xml
<uses-permission android:name="android.permission.BLUETOOTH" />
<uses-permission android:name="android.permission.BLUETOOTH_ADMIN" />
<uses-permission android:name="android.permission.BLUETOOTH_SCAN" />
<uses-permission android:name="android.permission.BLUETOOTH_CONNECT" />
<uses-permission android:name="android.permission.ACCESS_FINE_LOCATION" />
<uses-feature android:name="android.hardware.bluetooth_le" android:required="true" />
```

## Building

The plugin uses Gradle for building. The `build.gradle` file includes:

- Android SDK dependencies
- Proper namespace configuration (`unityble`)
- Release build configuration

All errors are logged and properly propagated to the Unity side.

## Migration from Previous Version

If you're upgrading from a previous version that used async scan methods:

**Old API:**
```csharp
var devices = await bleManager.StartScanAsync(TimeSpan.FromSeconds(10));
foreach (var device in devices)
{
    Debug.Log($"Device: {device.Name}");
}
```

**New API:**
```csharp
var discoveredDevices = new List<IBleDevice>();

bleManager.DeviceDiscovered += (device) => discoveredDevices.Add(device);
bleManager.ScanCompleted += () => {
    Debug.Log($"Found {discoveredDevices.Count} devices");
    foreach (var device in discoveredDevices)
    {
        Debug.Log($"Device: {device.Name}");
    }
};

bleManager.StartScan(TimeSpan.FromSeconds(10));
```

The new API provides better real-time performance and matches Android BLE's natural behavior.