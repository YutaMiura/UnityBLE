#ifndef iOSBlePluginPrivate_h
#define iOSBlePluginPrivate_h

#import "iOSBlePlugin.h"

@interface iOSBlePlugin (Private)

// Helper methods
- (CBPeripheral *)findPeripheralByAddress:(NSString *)address;
- (CBCharacteristic *)findCharacteristic:(NSString *)characteristicUUID inService:(NSString *)serviceUUID forDevice:(NSString *)address;
- (NSString *)peripheralToJSON:(CBPeripheral *)peripheral rssi:(NSNumber *)rssi;

// Timer callback
- (void)onScanTimeout;

@end

#endif /* iOSBlePluginPrivate_h */