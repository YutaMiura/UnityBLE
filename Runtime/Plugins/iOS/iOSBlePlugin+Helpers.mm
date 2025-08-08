#import "iOSBlePlugin.h"
#import "iOSBlePluginPrivate.h"

@implementation iOSBlePlugin (Helpers)

#pragma mark - Helper Methods

- (CBPeripheral *)findPeripheralByAddress:(NSString *)address {
    for (CBPeripheral *peripheral in self._discoveredPeripherals) {
        if ([peripheral.identifier.UUIDString isEqualToString:address]) {
            return peripheral;
        }
    }
    return nil;
}

- (CBCharacteristic *)findCharacteristic:(NSString *)characteristicUUID inService:(NSString *)serviceUUID forDevice:(NSString *)address {
    CBPeripheral *peripheral = self._connectedPeripherals[address];
    if (!peripheral) {
        return nil;
    }

    for (CBService *service in peripheral.services) {
        if ([service.UUID.UUIDString isEqualToString:serviceUUID]) {
            for (CBCharacteristic *characteristic in service.characteristics) {
                if ([characteristic.UUID.UUIDString isEqualToString:characteristicUUID]) {
                    return characteristic;
                }
            }
        }
    }

    return nil;
}

- (NSString *)peripheralToJSON:(CBPeripheral *)peripheral rssi:(NSNumber *)rssi {
    NSMutableDictionary *deviceDict = [[NSMutableDictionary alloc] init];

    deviceDict[@"name"] = peripheral.name ?: @"Unknown Device";
    deviceDict[@"address"] = peripheral.identifier.UUIDString;
    deviceDict[@"rssi"] = rssi ?: @(-50);
    deviceDict[@"isConnectable"] = @YES;
    deviceDict[@"txPower"] = @0;
    deviceDict[@"advertisingData"] = @"";
    deviceDict[@"isConnected"] = @(peripheral.state == CBPeripheralStateConnected);

    NSError *error;
    NSData *jsonData = [NSJSONSerialization dataWithJSONObject:deviceDict options:0 error:&error];
    if (error) {
        [self unityLog:[NSString stringWithFormat:@"[iOSBlePlugin] JSON serialization error: %@", error.localizedDescription]];
        return @"{}";
    }

    return [[NSString alloc] initWithData:jsonData encoding:NSUTF8StringEncoding];
}

- (void)onScanTimeout {
    [self unityLog:@"[iOSBlePlugin] Scan timeout reached"];
    [self stopScan];
}

@end