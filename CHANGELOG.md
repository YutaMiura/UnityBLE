# Changelog

All notable changes to this package will be documented in this file.

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