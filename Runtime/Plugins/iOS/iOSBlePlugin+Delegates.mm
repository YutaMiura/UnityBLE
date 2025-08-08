#import "iOSBlePlugin.h"
#import "iOSBlePluginPrivate.h"

@implementation iOSBlePlugin (Delegates)

#pragma mark - CBCentralManagerDelegate

- (void)centralManagerDidUpdateState:(CBCentralManager *)central {
    switch (central.state) {
        case CBManagerStatePoweredOn:
            [self unityLog:@"[iOSBlePlugin] Bluetooth powered on"];
            self._isBluetoothReady = YES;
            break;
        case CBManagerStatePoweredOff:
            [self unityLog:@"[iOSBlePlugin] Bluetooth powered off"];
            self._isBluetoothReady = NO;
            break;
        case CBManagerStateUnsupported:
            [self unityLog:@"[iOSBlePlugin] Bluetooth unsupported"];
            self._isBluetoothReady = NO;
            break;
        case CBManagerStateUnauthorized:
            [self unityLog:@"[iOSBlePlugin] Bluetooth unauthorized"];
            self._isBluetoothReady = NO;
            break;
        default:
            [self unityLog:@"[iOSBlePlugin] Bluetooth state unknown"];
            self._isBluetoothReady = NO;
            break;
    }
}

- (void)centralManager:(CBCentralManager *)central didDiscoverPeripheral:(CBPeripheral *)peripheral advertisementData:(NSDictionary<NSString *,id> *)advertisementData RSSI:(NSNumber *)RSSI {

    // Avoid duplicates
    for (CBPeripheral *existing in self._discoveredPeripherals) {
        if ([existing.identifier.UUIDString isEqualToString:peripheral.identifier.UUIDString]) {
            return;
        }
    }

    [self._discoveredPeripherals addObject:peripheral];

    [self unityLog:[NSString stringWithFormat:@"[iOSBlePlugin] Discovered peripheral: %@ (%@) RSSI: %@",
          peripheral.name ?: @"Unknown", peripheral.identifier.UUIDString, RSSI]];

    if (self.deviceDiscoveredCallback) {
        NSString *deviceJson = [self peripheralToJSON:peripheral rssi:RSSI];
        self.deviceDiscoveredCallback([deviceJson UTF8String]);
    }
}

- (void)centralManager:(CBCentralManager *)central didConnectPeripheral:(CBPeripheral *)peripheral {
    [self unityLog:[NSString stringWithFormat:@"[iOSBlePlugin] Connected to peripheral: %@", peripheral.identifier.UUIDString]];

    self._connectedPeripherals[peripheral.identifier.UUIDString] = peripheral;
    peripheral.delegate = self;

    // Discover services
    [peripheral discoverServices:nil];

    if (self.deviceConnectedCallback) {
        NSString *deviceJson = [self peripheralToJSON:peripheral rssi:nil];
        self.deviceConnectedCallback([deviceJson UTF8String]);
    }
}

- (void)centralManager:(CBCentralManager *)central didDisconnectPeripheral:(CBPeripheral *)peripheral error:(NSError *)error {
    [self unityLog:[NSString stringWithFormat:@"[iOSBlePlugin] Disconnected from peripheral: %@", peripheral.identifier.UUIDString]];

    [self._connectedPeripherals removeObjectForKey:peripheral.identifier.UUIDString];

    if (self.deviceDisconnectedCallback) {
        NSString *deviceJson = [self peripheralToJSON:peripheral rssi:nil];
        self.deviceDisconnectedCallback([deviceJson UTF8String]);
    }
}

- (void)centralManager:(CBCentralManager *)central didFailToConnectPeripheral:(CBPeripheral *)peripheral error:(NSError *)error {
    [self unityLog:[NSString stringWithFormat:@"[iOSBlePlugin] Failed to connect to peripheral: %@ Error: %@", peripheral.identifier.UUIDString, error.localizedDescription]];

    if (self.errorCallback) {
        NSString *errorMsg = [NSString stringWithFormat:@"Failed to connect to %@: %@", peripheral.identifier.UUIDString, error.localizedDescription];
        self.errorCallback([errorMsg UTF8String]);
    }
}

#pragma mark - CBPeripheralDelegate

- (void)peripheral:(CBPeripheral *)peripheral didDiscoverServices:(NSError *)error {
    if (error) {
        [self unityLog:[NSString stringWithFormat:@"[iOSBlePlugin] Error discovering services: %@", error.localizedDescription]];
        return;
    }

    [self unityLog:[NSString stringWithFormat:@"[iOSBlePlugin] Discovered %lu services for peripheral: %@", (unsigned long)peripheral.services.count, peripheral.identifier.UUIDString]];

    // Track pending characteristic discoveries
    NSNumber *pendingCount = @(peripheral.services.count);
    self._pendingServiceDiscovery[peripheral.identifier.UUIDString] = pendingCount;

    // Discover characteristics for all services
    for (CBService *service in peripheral.services) {
        [peripheral discoverCharacteristics:nil forService:service];
    }
}

- (void)peripheral:(CBPeripheral *)peripheral didDiscoverCharacteristicsForService:(CBService *)service error:(NSError *)error {
    if (error) {
        [self unityLog:[NSString stringWithFormat:@"[iOSBlePlugin] Error discovering characteristics: %@", error.localizedDescription]];
        return;
    }

    [self unityLog:[NSString stringWithFormat:@"[iOSBlePlugin] Discovered %lu characteristics for service: %@", (unsigned long)service.characteristics.count, service.UUID.UUIDString]];

    // Check if all services have been discovered
    NSString *deviceAddress = peripheral.identifier.UUIDString;
    NSNumber *pendingCount = self._pendingServiceDiscovery[deviceAddress];
    if (pendingCount) {
        NSInteger remaining = [pendingCount integerValue] - 1;
        if (remaining <= 0) {
            // All services have been discovered
            [self._pendingServiceDiscovery removeObjectForKey:deviceAddress];

            // Notify Unity that service discovery is complete
            if (self.servicesDiscoveredCallback) {
                self.servicesDiscoveredCallback([deviceAddress UTF8String]);
            }
            [self unityLog:[NSString stringWithFormat:@"[iOSBlePlugin] All services discovered for device: %@", deviceAddress]];
        } else {
            self._pendingServiceDiscovery[deviceAddress] = @(remaining);
        }
    }
}

- (void)peripheral:(CBPeripheral *)peripheral didUpdateValueForCharacteristic:(CBCharacteristic *)characteristic error:(NSError *)error {
    if (error) {
        [self unityLog:[NSString stringWithFormat:@"[iOSBlePlugin] Error reading characteristic: %@", error.localizedDescription]];
        if (self.errorCallback) {
            NSString *errorMsg = [NSString stringWithFormat:@"Error reading characteristic %@: %@", characteristic.UUID.UUIDString, error.localizedDescription];
            self.errorCallback([errorMsg UTF8String]);
        }
        return;
    }

    [self unityLog:[NSString stringWithFormat:@"[iOSBlePlugin] Characteristic value updated: %@ from device: %@", characteristic.UUID.UUIDString, peripheral.identifier.UUIDString]];

    if (self.characteristicValueCallback) {
        NSMutableDictionary *charDict = [[NSMutableDictionary alloc] init];
        charDict[@"uuid"] = characteristic.UUID.UUIDString;
        charDict[@"serviceUuid"] = characteristic.service.UUID.UUIDString;
        charDict[@"deviceAddress"] = peripheral.identifier.UUIDString;

        NSError *jsonError;
        NSData *jsonData = [NSJSONSerialization dataWithJSONObject:charDict options:0 error:&jsonError];
        NSString *charJson = jsonError ? @"{}" : [[NSString alloc] initWithData:jsonData encoding:NSUTF8StringEncoding];

        // Convert value to hex string
        NSString *valueHex = @"";
        if (characteristic.value) {
            const unsigned char *bytes = (const unsigned char *)[characteristic.value bytes];
            NSMutableString *hex = [[NSMutableString alloc] init];
            for (NSUInteger i = 0; i < characteristic.value.length; i++) {
                [hex appendFormat:@"%02x", bytes[i]];
            }
            valueHex = [hex copy];
        }

        self.characteristicValueCallback([charJson UTF8String], [valueHex UTF8String]);
    }
}

- (void)peripheral:(CBPeripheral *)peripheral didWriteValueForCharacteristic:(CBCharacteristic *)characteristic error:(NSError *)error {
    if (error) {
        [self unityLog:[NSString stringWithFormat:@"[iOSBlePlugin] Error writing characteristic: %@", error.localizedDescription]];
        if (self.errorCallback) {
            NSString *errorMsg = [NSString stringWithFormat:@"Error writing characteristic %@: %@", characteristic.UUID.UUIDString, error.localizedDescription];
            self.errorCallback([errorMsg UTF8String]);
        }
    } else {
        [self unityLog:[NSString stringWithFormat:@"[iOSBlePlugin] Characteristic written successfully: %@", characteristic.UUID.UUIDString]];
    }
}

- (void)peripheral:(CBPeripheral *)peripheral didUpdateNotificationStateForCharacteristic:(CBCharacteristic *)characteristic error:(NSError *)error {
    if (error) {
        [self unityLog:[NSString stringWithFormat:@"[iOSBlePlugin] Error updating notification state: %@", error.localizedDescription]];
        if (self.errorCallback) {
            NSString *errorMsg = [NSString stringWithFormat:@"Error updating notification state for %@: %@", characteristic.UUID.UUIDString, error.localizedDescription];
            self.errorCallback([errorMsg UTF8String]);
        }
    } else {
        BOOL isNotifying = characteristic.isNotifying;
        [self unityLog:[NSString stringWithFormat:@"[iOSBlePlugin] Notification state updated for characteristic %@: %@", characteristic.UUID.UUIDString, isNotifying ? @"ON" : @"OFF"]];
    }
}

@end