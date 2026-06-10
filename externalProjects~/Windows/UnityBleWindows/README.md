# UnityBleWindows (C++/WinRT native plugin)

Native BLE plugin that backs the Windows implementation of UnityBLE
(`Runtime/internal/Windows/`). It implements scanning, connect/disconnect,
service & characteristic discovery, read, write and notify using the WinRT
`Windows.Devices.Bluetooth` APIs, and exposes them as C-style exports
(`UnityBLEWin_*`) consumed by `WindowsBleNativePlugin.cs`.

## Requirements
- Windows 10 version 1809 (build 17763) or later.
- **Windows 10/11 SDK** — mandatory regardless of compiler. It provides the
  C++/WinRT projection headers (`Include\<ver>\cppwinrt\winrt\...`) and
  `WindowsApp.lib`. Without it neither clang nor MSVC can build this DLL.
- A C++ compiler: either **clang-cl** ("C++ Clang tools for Windows" VS
  component, or standalone LLVM) **or** MSVC.
- Target architecture: **x64 only**.

## Build option A — clang-cl, no CMake (recommended here)

`build.bat` locates Visual Studio via `vswhere`, runs `vcvars64.bat` to put the
Windows SDK / CRT on the include & lib paths, then compiles with `clang-cl`
(C++20, so the standard `<coroutine>` header is used — no `-fcoroutines-ts`).

```bat
cd externalProjects~\Windows\UnityBleWindows
build.bat
```

The output is `build\UnityBleWindows.dll`.

If you prefer to run it manually from a "x64 Native Tools Command Prompt for VS":
```bat
clang-cl /std:c++20 /EHsc /MD /LD /O2 ^
    /DWIN32_LEAN_AND_MEAN /DNOMINMAX /DUNICODE /D_UNICODE ^
    src\UnityBleWindows.cpp /Fe:build\UnityBleWindows.dll /link WindowsApp.lib
```

## Build option B — CMake (MSVC or clang)

From a "x64 Native Tools Command Prompt for VS" (so the SDK `cppwinrt` headers
are on the include path):

```bat
cd externalProjects~\Windows\UnityBleWindows
cmake -B build -A x64
cmake --build build --config Release
```

The output is `build\Release\UnityBleWindows.dll`.

## Install into the Unity package

1. Copy `UnityBleWindows.dll` to:
   `Runtime/Plugins/Windows/x86_64/UnityBleWindows.dll`
2. In Unity, select the DLL and configure the Plugin Inspector:
   - Platform: **Editor** and **Standalone** enabled.
   - CPU: **x86_64**.
   - OS (Editor settings): **Windows**.
3. Re-import / restart the Editor on Windows. `BleManager.Instance.Scanner`
   will now use `WindowsBleScanner`.

## Native contract notes
- All values delivered to `OnValueReceived` carry the **characteristic UUID**
  (lower-case canonical form) as the first argument and the value as a
  **Base64** string. This matches how the C# read/subscribe commands route
  results through `BleDeviceEvents.OnDataReceived`.
- Peripheral `uuid` is the 48-bit Bluetooth address formatted as 12 lowercase
  hex digits (e.g. `aabbccddeeff`). `ConnectToPeripheral` parses it back.
- Scan returns `0` ok / `1` already scanning / `-1` powered off / `-2`
  unauthorized / `-3` unsupported, matching the C# error mapping.
- Encrypted characteristics may require the device to be paired in Windows
  Settings; basic GATT access does not.
