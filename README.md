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
