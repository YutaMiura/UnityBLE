# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

# Project Overview

This is a Unity UPM package for Bluetooth Low Energy (BLE) communication across multiple platforms. The package provides a unified interface for BLE operations while implementing platform-specific native plugins for Android, iOS, and macOS.

# Architecture

## Core Design Pattern
- **Command Pattern**: All BLE operations are implemented as commands with async/await support and cancellation tokens
- **Factory Pattern**: `BleCommunicatorFactory` creates platform-specific implementations
- **Event-Driven Architecture**: Uses events for device discovery, connection status, and data notifications
- **Universal Base Classes**: `UniversalBleDevice` provides common functionality across all platforms

## Platform Structure
```
Runtime/
├── BleManager.cs                    # Legacy singleton manager
├── interfaces/                      # Core interfaces and base classes
│   ├── IBleDevice.cs               # Main device interface
│   ├── UniversalBleDevice.cs       # Abstract base implementation
│   ├── Commands/                   # Command pattern interfaces
│   └── exceptions/                 # Custom exceptions
└── internal/                       # Platform-specific implementations
    ├── Android/                    # Android BLE via Java plugin
    ├── iOS/                        # iOS BLE via Objective-C plugin
    ├── macOS/                      # macOS BLE for Unity Editor
    └── Windows/                    # Windows BLE via C++/WinRT plugin (Editor + Standalone)
```

## Key Components

### Device Management
- `UniversalBleDevice`: Abstract base class with common BLE functionality
- Platform-specific implementations: `AndroidBleDevice`, `iOSBleDevice`, `MacOSBleDevice`
- Each platform implements connection, service discovery, and characteristic operations

### Command Pattern Implementation
- Each BLE operation is a command (Connect, Disconnect, Scan, Read, Write, Subscribe)
- Commands support `CancellationToken` for operation cancellation
- Platform-specific command implementations in `Commands/` folders

### Native Plugin Integration
- **Android**: Java plugin with Unity message passing
- **iOS**: Objective-C plugin with C# P/Invoke
- **macOS**: Objective-C++ bundle for Unity Editor testing
- **Windows**: C++/WinRT DLL with C# P/Invoke (Editor + Standalone)

## Critical Architecture Notes

1. **Platform Conditional Compilation**: Uses `#if` directives for platform-specific code
2. **Event Subscription Management**: Devices must subscribe/unsubscribe to native events
3. **Service Discovery**: Services are cached in device objects after connection
4. **Characteristic Notifications**: Handled through event callbacks from native plugins

# Development Commands

## macOS Bundle Build (Required for Editor Testing)
```bash
cd Assets/UnityBLE/Runtime/internal/macOS/Plugins
clang -dynamiclib -framework CoreBluetooth -framework Foundation -o MacOSBlePlugin.bundle MacOSBlePlugin.mm -arch x86_64 -arch arm64
```

**Important**: After building the bundle:
1. Restart Unity Editor
2. Configure plugin settings in Unity Inspector:
   - Select MacOSBlePlugin.bundle in Project window
   - Set Platform: macOS
   - Enable Editor checkbox
   - Set CPU: AnyCPU

## Windows DLL Build (Required for Editor / Standalone on Windows)
Source project: `externalProjects~/Windows/UnityBleWindows/` (C++/WinRT).
A **Windows 10/11 SDK is mandatory** (provides the C++/WinRT headers and
`WindowsApp.lib`); clang alone is not sufficient.

- clang-cl, no CMake (recommended): run `externalProjects~\Windows\UnityBleWindows\build.bat`
  → output `build\UnityBleWindows.dll`.
- CMake (MSVC/clang) from a "x64 Native Tools Command Prompt for VS":
  ```bat
  cd externalProjects~\Windows\UnityBleWindows
  cmake -B build -A x64
  cmake --build build --config Release
  ```

Then copy the produced `UnityBleWindows.dll` to
`Runtime/Plugins/Windows/x86_64/UnityBleWindows.dll` and, in the Unity Plugin
Inspector, enable **Editor** + **Standalone**, set CPU **x86_64** and Editor OS
**Windows**. Restart the Editor. See the project README for details.

## Unity Editor Log Locations
- **macOS**: `~/Library/Logs/Unity/Editor.log`
- **Windows**: `C:\Users\username\AppData\Local\Unity\Editor\Editor.log`

## Package Information
- **Package Name**: `com.yutamiura.unityble`
- **Unity Version**: 2020.3+
- **Current Environment**: Unity6 (6000.0.47f1)
- **UI System**: UI-ToolKit

# Code Conventions

## Field Naming
- Private fields must start with `_` (underscore)
- Example: `private bool _isConnected;`

## Platform-Specific Code Patterns
```csharp
#if UNITY_ANDROID && !UNITY_EDITOR
    // Android implementation
#elif UNITY_IOS && !UNITY_EDITOR
    // iOS implementation
#elif UNITY_EDITOR_OSX
    // macOS Editor implementation
#endif
```

## Async/Await Patterns
- All BLE operations are async with CancellationToken support
- Use `cancellationToken.ThrowIfCancellationRequested()` at operation start
- Wrap operations in try-catch for proper error handling

## Event Pattern
```csharp
public BleDeviceEvents Events { get; } = new BleDeviceEvents();
// Usage: Events.RaiseConnected(this);
```

# Testing Strategy

## Platform Testing
- **Android**: Real device testing required
- **iOS**: Real device testing required
- **macOS**: Unity Editor testing with bundle plugin
- **Windows**: Unity Editor / Standalone testing with the C++/WinRT DLL

## Native Plugin Dependencies
- **Android**: Requires BLUETOOTH and BLUETOOTH_ADMIN permissions
- **iOS**: Requires NSBluetoothAlwaysUsageDescription in Info.plist
- **macOS**: Requires CoreBluetooth framework access
- **Windows**: Requires Windows 10 1809+ and a Bluetooth LE radio (WinRT
  `Windows.Devices.Bluetooth`)

# Common Issues

## macOS Editor
- "Failed to initialize" errors indicate missing or misconfigured bundle
- Bundle must be rebuilt after any changes to .mm files
- Unity Editor restart required after bundle changes
### For UnityEditor on MacOS
- We need a bundle file to use BLE function on Unity Editor.
  - We have mm file in project, so you can create a bundle file with command below.
``` shell
 cd Assets/UnityBLE/Runtime/internal/macOS/Plugins
clang -dynamiclib -framework CoreBluetooth -framework Foundation -o MacOSBlePlugin.bundle MacOSBlePlugin.mm -arch x86_64 -arch arm64
```
  #### Command breakdown:
  - clang -dynamiclib: Creates a dynamic library (bundle)
  - -framework CoreBluetooth: Links CoreBluetooth framework for BLE
  functionality
  - -framework Foundation: Links Foundation framework for basic
  Objective-C classes
  - -o MacOSBlePlugin.bundle: Output file name (Unity recognizes
  .bundle extension)
  - -arch x86_64 -arch arm64: Creates universal binary for both
  Intel and Apple Silicon Macs

  After running this command:
  1. Restart Unity Editor (required for Unity to recognize the new
  bundle)
  2. Configure plugin settings in Unity Inspector:
    - Select MacOSBlePlugin.bundle in Project window
    - Set Platform: macOS
    - Enable Editor checkbox
    - Set CPU: AnyCPU

  This enables real BLE functionality in Unity Editor on macOS
  instead of getting "Failed to initialize" errors.

## Platform Compilation
- Ensure proper conditional compilation directives
- Platform-specific assemblies prevent unnecessary dependencies
- Command implementations must match platform capabilities

# Important Files

## Core Interfaces
- `Runtime/interfaces/IBleDevice.cs` - Main device interface
- `Runtime/interfaces/UniversalBleDevice.cs` - Base device implementation
- `Runtime/BleManager.cs` - Legacy singleton manager

## Platform Entry Points
- `Runtime/internal/Android/AndroidBleScanner.cs`
- `Runtime/internal/Apple/AppleBleScanner.cs`
- `Runtime/internal/Windows/WindowsBleScanner.cs`

## Native Plugins
- `Runtime/Plugins/Android/UnityBLE.aar` - Android (Kotlin); source in `externalProjects~/Android`
- `Runtime/Plugins/iOS/UnityBlePlugin.framework` - Apple (Swift); source in `externalProjects~/Xcode`
- `Runtime/Plugins/macOS/UnityBlePluginBundle.bundle` - macOS Editor (Swift); source in `externalProjects~/Xcode`
- `Runtime/Plugins/Windows/x86_64/UnityBleWindows.dll` - Windows (C++/WinRT); source in `externalProjects~/Windows`


# Basics
- private field have to start with _.
- We use Unity6 for environment.
  - Unity version is 6000.0.47f1
  - Sample ploject has UI built with UI-ToolKit.
    - This is UI-ToolKit documentation. You can know how to build a UI system in UI-ToolKit.
      - https://docs.unity3d.com/6000.0/Documentation/Manual/UIElements.html

# Log file for UnityEditor
 - for macOS
   - ~/Library/Logs/Unity/Editor.log
 - for Windows
   - C:\Users\username\AppData\Local\Unity\Editor\Editor.log
