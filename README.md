# Unity BLE

A Unity UPM package for Bluetooth Low Energy (BLE) connection support.

## Features
- BLE device scanning with event-driven architecture
- BLE connection and disconnection management
- Service and characteristic discovery
- Data read/write support
- Full cancellation support with CancellationToken
- Real-time device discovery and connection events

## Dependencies
This libraly has dependencies below so you need add these from gradle.
  - `androidx.activity:activity-ktx:1.10.1`
    - This library has a custom activity for request permision.
  - `org.jetbrains.kotlin:kotlin-stdlib:2.0.20`
    - This library use kotlin.
  - `org.jetbrains.kotlinx:kotlinx-serialization-json:1.7.0`
    - Serialize a object to comunicate with Unity.
1. Create a custom gradle template from Unity.
2. Add dependencies to custom gradle file.
   ```
   ...
    dependencies {
        ...
        implementation 'androidx.activity:activity-ktx:1.10.1'
        implementation "org.jetbrains.kotlin:kotlin-stdlib:2.0.20"
        implementation "org.jetbrains.kotlinx:kotlinx-serialization-json:1.7.0"
        ...
    }
   ```
3. Apply plugin
   ```
    // You have to write before apply section.
    plugins {
        kotlin("plugin.serialization") version "1.7.0"
    }
    ...
    apply plugin ...
   ```

## Windows (Unity Editor / Standalone)
Windows BLE is provided by a C++/WinRT native DLL (`Windows.Devices.Bluetooth`).
- Requirements: Windows 10 1809+ with a Bluetooth LE radio, a **Windows 10/11
  SDK** (mandatory — supplies the C++/WinRT headers and `WindowsApp.lib`), and a
  C++ compiler (clang-cl or MSVC).
- Build the DLL from `externalProjects~/Windows/UnityBleWindows/`:
  ```bat
  cd externalProjects~\Windows\UnityBleWindows
  build.bat            REM clang-cl, no CMake (recommended)
  ```
  CMake is also supported (`cmake -B build -A x64 && cmake --build build --config Release`).
- Copy the produced `UnityBleWindows.dll` to
  `Runtime/Plugins/Windows/x86_64/UnityBleWindows.dll`, then in the Unity Plugin
  Inspector enable **Editor** + **Standalone**, CPU **x86_64**, Editor OS
  **Windows**, and restart the Editor.

See `externalProjects~/Windows/UnityBleWindows/README.md` for full details.
