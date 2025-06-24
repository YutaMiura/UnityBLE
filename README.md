# Unity BLE

A Unity UPM package for Bluetooth Low Energy (BLE) connection support.

## Features
- BLE device scanning with event-driven architecture
- BLE connection and disconnection management
- Service and characteristic discovery
- Data read/write support
- Full cancellation support with CancellationToken
- Real-time device discovery and connection events

## Installation
1. Open your Unity project.
2. Open `Packages/manifest.json`.
3. Add the following line to the `dependencies` section:
   ```json
   "com.yutamiura.unityble": "https://github.com/YOUR_GITHUB_USERNAME/UnityBLE.git"
   ```
4. Save the file. Unity will automatically install the package.

## Basic Usage

### 1. Initialize BLE Communicator

```csharp
using UnityBLE;
using UnityBLE.Factory;
using System;
using System.Threading;
using UnityEngine;

public class BleExample : MonoBehaviour
{
    private IBLECommunicator bleCommunicator;
    private CancellationTokenSource initCts;

    async void Start()
    {
        try
        {
            // Check if BLE is supported on this platform
            if (!BleCommunicatorFactory.IsSupported())
            {
                Debug.LogError("BLE is not supported on this platform");
                return;
            }

            // Create a BLE communicator instance
            bleCommunicator = BleCommunicatorFactory.Create();

            // Create cancellation token for initialization
            initCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            // Initialize BLE communicator with timeout
            await bleCommunicator.InitializeAsync(initCts.Token);
            Debug.Log("BLE initialized successfully");
        }
        catch (OperationCanceledException)
        {
            Debug.LogError("BLE initialization was cancelled or timed out");
        }
        catch (Exception e)
        {
            Debug.LogError($"BLE initialization failed: {e.Message}");
        }
    }

    void OnDestroy()
    {
        initCts?.Cancel();
        initCts?.Dispose();
    }
}
```

### 2. Device Scanning with Cancellation

```csharp
using UnityBLE;
using UnityBLE.Factory;
using System;
using System.Threading;
using UnityEngine;

public class BleScanningExample : MonoBehaviour
{
    private IBLECommunicator bleCommunicator;
    private CancellationTokenSource scanCts;

        void Start()
    {
        // Create a BLE communicator instance
        bleCommunicator = BleCommunicatorFactory.Create();

        // Subscribe to scan events
        bleCommunicator.DeviceDiscovered += OnDeviceDiscovered;
        bleCommunicator.ScanCompleted += OnScanCompleted;
        bleCommunicator.ScanFailed += OnScanFailed;
    }

    public void StartScanning()
    {
        // Create cancellation token that allows manual cancellation
        scanCts = new CancellationTokenSource();

        // Start scanning for 30 seconds (can be cancelled manually)
        bleCommunicator.StartScan(TimeSpan.FromSeconds(30), scanCts.Token);
    }

    public void StopScanning()
    {
        // Cancel the scan operation
        scanCts?.Cancel();
        bleCommunicator.StopScan();
    }

    private void OnDeviceDiscovered(IBleDevice device)
    {
        Debug.Log($"Device discovered: {device.Name} ({device.Address})");
    }

    private void OnScanCompleted()
    {
        Debug.Log("Scan completed");
        scanCts?.Dispose();
    }

    private void OnScanFailed(string error)
    {
        Debug.LogError($"Scan failed: {error}");
        scanCts?.Dispose();
    }

    void OnDestroy()
    {
        scanCts?.Cancel();
        scanCts?.Dispose();
    }
}
```

### 3. Device Connection with Cancellation

```csharp
using UnityBLE;
using UnityBLE.Factory;
using System;
using System.Threading;
using UnityEngine;

public class BleConnectionExample : MonoBehaviour
{
    private IBLECommunicator bleCommunicator;
    private IBleDevice targetDevice;
    private CancellationTokenSource connectionCts;

        void Start()
    {
        // Create a BLE communicator instance
        bleCommunicator = BleCommunicatorFactory.Create();

        // Subscribe to discovery and connection events
        bleCommunicator.DeviceDiscovered += OnDeviceDiscovered;

        // For connection events, cast to platform-specific type
        if (bleCommunicator is AndroidBleCommunicator androidCommunicator)
        {
            androidCommunicator.DeviceConnected += OnDeviceConnected;
            androidCommunicator.DeviceDisconnected += OnDeviceDisconnected;
            androidCommunicator.ConnectionFailed += OnConnectionFailed;
        }
    }

    private void OnDeviceDiscovered(IBleDevice device)
    {
        if (device.Name == "MyTargetDevice")
        {
            targetDevice = device;
            ConnectToDevice();
        }
    }

    private async void ConnectToDevice()
    {
        if (targetDevice == null) return;

        try
        {
            // Create cancellation token with 15 second timeout
            connectionCts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

            Debug.Log($"Connecting to {targetDevice.Name}...");
            await targetDevice.ConnectAsync(connectionCts.Token);
            Debug.Log("Connected successfully!");
        }
        catch (OperationCanceledException)
        {
            Debug.LogError("Connection was cancelled or timed out");
        }
        catch (Exception e)
        {
            Debug.LogError($"Connection failed: {e.Message}");
        }
        finally
        {
            connectionCts?.Dispose();
        }
    }

    private async void DisconnectFromDevice()
    {
        if (targetDevice == null) return;

        try
        {
            // Create cancellation token with 10 second timeout
            using var disconnectCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            Debug.Log($"Disconnecting from {targetDevice.Name}...");
            await targetDevice.DisconnectAsync(disconnectCts.Token);
            Debug.Log("Disconnected successfully!");
        }
        catch (OperationCanceledException)
        {
            Debug.LogError("Disconnection was cancelled or timed out");
        }
        catch (Exception e)
        {
            Debug.LogError($"Disconnection failed: {e.Message}");
        }
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

    void OnDestroy()
    {
        connectionCts?.Cancel();
        connectionCts?.Dispose();
    }
}
```

### 4. Service Discovery with Cancellation

```csharp
using UnityBLE;
using UnityBLE.Factory;
using System;
using System.Threading;
using UnityEngine;

public class BleServiceExample : MonoBehaviour
{
    private IBLECommunicator bleCommunicator;
    private IBleDevice connectedDevice;
    private CancellationTokenSource serviceCts;

        void Start()
    {
        bleCommunicator = BleCommunicatorFactory.Create();

        if (bleCommunicator is AndroidBleCommunicator androidCommunicator)
        {
            androidCommunicator.DeviceConnected += OnDeviceConnected;
        }
    }

    private async void OnDeviceConnected(string deviceAddress)
    {
        // Find the connected device using platform-specific communicator
        if (bleCommunicator is AndroidBleCommunicator androidCommunicator)
        {
            connectedDevice = androidCommunicator.GetConnectedDevice(deviceAddress);

            if (connectedDevice != null)
            {
                await DiscoverServices();
            }
        }
    }

    private async System.Threading.Tasks.Task DiscoverServices()
    {
        try
        {
            // Create cancellation token with 20 second timeout
            serviceCts = new CancellationTokenSource(TimeSpan.FromSeconds(20));

            Debug.Log("Discovering services...");
            var services = await connectedDevice.GetServicesAsync(serviceCts.Token);

            Debug.Log($"Found {services.Count} services:");
            foreach (var service in services)
            {
                Debug.Log($"Service: {service.Uuid}");
                await DiscoverCharacteristics(service);
            }
        }
        catch (OperationCanceledException)
        {
            Debug.LogError("Service discovery was cancelled or timed out");
        }
        catch (Exception e)
        {
            Debug.LogError($"Service discovery failed: {e.Message}");
        }
        finally
        {
            serviceCts?.Dispose();
        }
    }

    private async System.Threading.Tasks.Task DiscoverCharacteristics(IBleService service)
    {
        try
        {
            // Create cancellation token with 10 second timeout
            using var charCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var characteristics = await service.GetCharacteristicsAsync(charCts.Token);
            Debug.Log($"Found {characteristics.Count} characteristics in service {service.Uuid}");
        }
        catch (OperationCanceledException)
        {
            Debug.LogError($"Characteristic discovery for service {service.Uuid} was cancelled");
        }
        catch (Exception e)
        {
            Debug.LogError($"Characteristic discovery failed: {e.Message}");
        }
    }

    void OnDestroy()
    {
        serviceCts?.Cancel();
        serviceCts?.Dispose();
    }
}
```

### 5. Data Operations with Cancellation

```csharp
using UnityBLE;
using UnityBLE.Factory;
using System;
using System.Threading;
using UnityEngine;

public class BleDataExample : MonoBehaviour
{
    private IBleCharacteristic targetCharacteristic;
    private CancellationTokenSource operationCts;

    private async void ReadCharacteristic()
    {
        if (targetCharacteristic == null) return;

        try
        {
            // Create cancellation token with 5 second timeout
            operationCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            Debug.Log("Reading characteristic...");
            byte[] data = await targetCharacteristic.ReadAsync(operationCts.Token);
            Debug.Log($"Read {data.Length} bytes from characteristic");
        }
        catch (OperationCanceledException)
        {
            Debug.LogError("Read operation was cancelled or timed out");
        }
        catch (Exception e)
        {
            Debug.LogError($"Read operation failed: {e.Message}");
        }
        finally
        {
            operationCts?.Dispose();
        }
    }

    private async void WriteCharacteristic(byte[] data)
    {
        if (targetCharacteristic == null) return;

        try
        {
            // Create cancellation token with 5 second timeout
            using var writeCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            Debug.Log("Writing to characteristic...");
            await targetCharacteristic.WriteAsync(data, true, writeCts.Token);
            Debug.Log("Write operation completed");
        }
        catch (OperationCanceledException)
        {
            Debug.LogError("Write operation was cancelled or timed out");
        }
        catch (Exception e)
        {
            Debug.LogError($"Write operation failed: {e.Message}");
        }
    }

    private async void SubscribeToNotifications()
    {
        if (targetCharacteristic == null) return;

        try
        {
            // Create cancellation token with 5 second timeout for subscription setup
            using var subscribeCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            Debug.Log("Subscribing to notifications...");
            await targetCharacteristic.SubscribeAsync(OnNotificationReceived, subscribeCts.Token);
            Debug.Log("Subscribed to notifications");
        }
        catch (OperationCanceledException)
        {
            Debug.LogError("Subscribe operation was cancelled or timed out");
        }
        catch (Exception e)
        {
            Debug.LogError($"Subscribe operation failed: {e.Message}");
        }
    }

    private void OnNotificationReceived(byte[] data)
    {
        Debug.Log($"Notification received: {data.Length} bytes");
    }

    void OnDestroy()
    {
        operationCts?.Cancel();
        operationCts?.Dispose();
    }
}
```

## Advanced Features

### Cancellation Token Sources Management

```csharp
using UnityBLE;
using UnityBLE.Factory;
using System;
using System.Threading;
using UnityEngine;

public class CancellationManager : MonoBehaviour
{
    private IBLECommunicator bleCommunicator;
    private CancellationTokenSource masterCts;

        void Start()
    {
        // Create a BLE communicator instance
        bleCommunicator = BleCommunicatorFactory.Create();

        // Create a master cancellation token for the entire session
        masterCts = new CancellationTokenSource();
    }

    private async void PerformBleOperations()
    {
        try
        {
            // Create a linked token that respects both operation timeout and master cancellation
            using var operationCts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                masterCts.Token, operationCts.Token);

            // All operations will be cancelled if master token is cancelled
            // or if operation times out
            await bleCommunicator.InitializeAsync(linkedCts.Token);
            // ... other operations
        }
        catch (OperationCanceledException)
        {
            Debug.Log("BLE operations were cancelled");
        }
    }

    public void CancelAllOperations()
    {
        // Cancel all ongoing BLE operations
        masterCts?.Cancel();
    }

    void OnDestroy()
    {
        masterCts?.Cancel();
        masterCts?.Dispose();
    }
}
```

## Platform Support

### Getting the Communicator

```csharp
using UnityBLE;
using UnityBLE.Factory;

// Check platform support
if (BleCommunicatorFactory.IsSupported())
{
    // Create a communicator instance
    IBLECommunicator communicator = BleCommunicatorFactory.Create();

    // Platform information
    Debug.Log($"Platform: {BleCommunicatorFactory.GetPlatformName()}");

    // Use platform-specific features if needed
    if (communicator is AndroidBleCommunicator androidCommunicator)
    {
        // Android-specific operations
        bool isConnected = androidCommunicator.IsDeviceConnected("device_address");
    }
}
```

## Migration from Previous Version

If you were using the old async-return API, here's how to migrate:

### Old API (deprecated):
```csharp
// Old way - returned Task<IReadOnlyList<IBleDevice>>
var devices = await bleManager.ScanAsync(TimeSpan.FromSeconds(10));
```

### New API (current):
```csharp
// New way - event-driven with cancellation support
var communicator = BleCommunicatorFactory.Create();
communicator.DeviceDiscovered += OnDeviceFound;
communicator.ScanCompleted += OnScanComplete;
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
communicator.StartScan(TimeSpan.FromSeconds(10), cts.Token);
```

## Error Handling

The BLE operations can throw several types of exceptions:
- `OperationCanceledException`: When operations are cancelled via CancellationToken
- `InvalidOperationException`: When operations are called in invalid states
- `UnauthorizedAccessException`: When Bluetooth permissions are denied
- `Exception`: For other BLE-related errors

Always wrap BLE operations in try-catch blocks and handle cancellation appropriately.

## License
MIT