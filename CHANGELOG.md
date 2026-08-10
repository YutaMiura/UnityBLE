# Changelog

All notable changes to this package will be documented in this file.

## [0.3.3]
### Added
- `IBleCharacteristic.SubscribeAsync(CancellationToken)` — subscribes and completes once
  notifications are actually live on the device, so the first command cannot race the
  subscription. `Subscribe()` is unchanged and still returns at the request.
- Android: `onDescriptorWrite` is now forwarded to managed code (`OnDescriptorWrite`), which
  is what `SubscribeAsync` awaits. Every early exit in `subscribe()` reports a failed
  descriptor write too, so a waiter fails immediately instead of timing out.

### Fixed
- A connect whose first `connectGatt` attempt failed (Android reports GATT 133 as
  `STATE_DISCONNECTED`) came back "connected" with no services discovered. The peripheral's
  own `OnDisconnected` treated the failed attempt as a lost link and unsubscribed itself, so
  when `ConnectCommand` retried and succeeded, `OnConnected` — and the `DiscoverServices()`
  it performs — never ran. Callers then waited out their characteristic-discovery timeout on
  a link that was actually up. Disconnects arriving during a connect no longer tear the
  subscriptions down; retries own that case.
- Android: `BleManager.write()` no longer discards the status code returned by
  `BluetoothGatt.writeCharacteristic` on API 33+. It reported every write as `OK`, so a write
  the stack refused — most often `ERROR_GATT_WRITE_REQUEST_BUSY`, because enabling
  notifications leaves a CCCD descriptor write in flight — looked like a successful send and
  the command was silently lost. Callers that wrote immediately after `Subscribe()` therefore
  lost their first command with no error on any layer. `WriteResult.WRITE_REQUEST_BUSY` was
  added for that case (appended last, so existing ordinals keep their meaning).

## [0.3.2]
### Fixed
- Android: `ensurePermissionsWithResult` now returns `ReadyForUse` immediately when every
  requested permission is already granted, before checking whether the location service is
  enabled. On devices that report the location service as disabled but still grant
  `ACCESS_COARSE_LOCATION` — Android 9 TV boxes in particular — the previous order returned
  `LocationServiceDisabled` and scanning could never start.

### Changed
- Android: the library is built with the Java/Kotlin 17 toolchain (was 11), so `UnityBLE.aar`
  now contains class file version 61.
- The declared minimum Unity version is now `6000.0` (was `2020.3`). Class file version 61
  needs the JDK 17 / Gradle 8 toolchain that Unity 6 bundles; 2021.3 and 2022.3 ship JDK 11
  with Gradle 7.x. The package already required 2021.2 or newer in practice, because the C#
  uses target-typed `new()`.

## [1.0.0] - Initial release
- Initial project structure and setup for Unity BLE UPM package.