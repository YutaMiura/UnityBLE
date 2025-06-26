# macOS BLE Plugin for Unity

This plugin provides Bluetooth Low Energy (BLE) functionality for Unity applications running on macOS, both in the Unity Editor and standalone builds.

## Features

- **Real BLE Device Scanning**: Uses Core Bluetooth framework to discover actual BLE peripherals
- **Device Connection Management**: Connect and disconnect from BLE devices
- **Service Discovery**: Enumerate available services on connected devices
- **Characteristic Discovery**: Find characteristics within services
- **Read/Write Operations**: Perform BLE read and write operations
- **Event-Driven Architecture**: Asynchronous callbacks for device events

## Architecture

### Native Plugin (`MacOSBlePlugin.mm`)
- Objective-C++ wrapper around Core Bluetooth framework
- Handles CBCentralManager and CBPeripheral operations
- Provides C interface for Unity interop
- Manages device discovery, connection, and data operations

### Unity Bridge (`MacOSBleNativePlugin.cs`)
- C# wrapper for native plugin functions
- Event system for async operations
- Platform-specific conditional compilation
- Memory management for marshaled data

### Command Pattern Implementation
- `MacOSScanCommand` - BLE device scanning
- `MacOSConnectDeviceCommand` - Device connection
- `MacOSDisconnectDeviceCommand` - Device disconnection
- `MacOSGetServicesCommand` - Service discovery
- `MacOSGetCharacteristicsCommand` - Characteristic discovery

## Requirements

### macOS Permissions
Add the following to your app's Info.plist for macOS builds:

```xml
<key>NSBluetoothAlwaysUsageDescription</key>
<string>This app uses Bluetooth to communicate with BLE devices.</string>
<key>NSBluetoothPeripheralUsageDescription</key>
<string>This app uses Bluetooth to communicate with BLE peripherals.</string>
```

### Unity Build Settings
1. Set target platform to macOS
2. Ensure Scripting Backend is set to Mono or IL2CPP
3. Add CoreBluetooth.framework to Additional Dependencies

## Usage

### Basic Scanning
```csharp
// Initialize the plugin
MacOSBleNativePlugin.Initialize();

// Subscribe to events
MacOSBleNativePlugin.OnDeviceDiscovered += (deviceJson) => {
    Debug.Log($"Found device: {deviceJson}");
};

// Start scanning for 10 seconds
MacOSBleNativePlugin.StartScan(10.0);
```

### Device Connection
```csharp
// Connect to a device by UUID
string deviceAddress = "12345678-1234-1234-1234-123456789ABC";
MacOSBleNativePlugin.ConnectToDevice(deviceAddress);

// Subscribe to connection events
MacOSBleNativePlugin.OnDeviceConnected += (deviceJson) => {
    Debug.Log($"Connected to: {deviceJson}");
};
```

### Service Discovery
```csharp
// Get services for connected device
string[] services = MacOSBleNativePlugin.GetServices(deviceAddress);
foreach (string serviceUUID in services) {
    Debug.Log($"Service: {serviceUUID}");
}
```

## Platform Conditional Compilation

The plugin uses conditional compilation to ensure it only runs on macOS:

```csharp
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
    // macOS-specific code
#else
    // Stub implementations for other platforms
#endif
```

## Debugging

Enable detailed logging by checking Unity Console. All BLE operations are logged with `[macOS BLE]` prefix.

## Limitations

- Only works on macOS (Unity Editor and standalone builds)
- Requires macOS 10.7+ with Bluetooth 4.0+ support
- BLE Central role only (cannot act as peripheral)
- Some advanced BLE features may not be implemented

## Thread Safety

All native plugin calls are marshaled to the main thread. Unity callbacks are executed on the main thread to ensure UI thread safety.