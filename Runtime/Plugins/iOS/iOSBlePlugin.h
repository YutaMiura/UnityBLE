#ifndef iOSBlePlugin_h
#define iOSBlePlugin_h

#import <Foundation/Foundation.h>
#import <CoreBluetooth/CoreBluetooth.h>
#import "iOSBlePluginTypes.h"

@interface iOSBlePlugin : NSObject <CBCentralManagerDelegate, CBPeripheralDelegate>

// Core Bluetooth properties
@property (nonatomic, strong) CBCentralManager *_centralManager;
@property (nonatomic, strong) NSMutableArray<CBPeripheral *> *_discoveredPeripherals;
@property (nonatomic, strong) NSMutableDictionary<NSString *, CBPeripheral *> *_connectedPeripherals;
@property (nonatomic, strong) NSMutableDictionary<NSString *, NSNumber *> *_pendingServiceDiscovery;

// State properties
@property (nonatomic, assign) BOOL _isScanning;
@property (nonatomic, strong) NSTimer *_scanTimer;
@property (nonatomic, assign) BOOL _isBluetoothReady;

// Unity callbacks
@property (nonatomic, assign) UnityDeviceDiscoveredCallback deviceDiscoveredCallback;
@property (nonatomic, assign) UnityDeviceConnectedCallback deviceConnectedCallback;
@property (nonatomic, assign) UnityDeviceDisconnectedCallback deviceDisconnectedCallback;
@property (nonatomic, assign) UnityScanCompletedCallback scanCompletedCallback;
@property (nonatomic, assign) UnityCharacteristicValueCallback characteristicValueCallback;
@property (nonatomic, assign) UnityErrorCallback errorCallback;
@property (nonatomic, assign) UnityLogCallback logCallback;
@property (nonatomic, assign) UnityServicesDiscoveredCallback servicesDiscoveredCallback;

// Singleton instance
+ (iOSBlePlugin *)sharedInstance;

// Public methods
- (BOOL)initialize;
- (BOOL)waitForBluetoothReady:(NSTimeInterval)timeout;
- (BOOL)startScanWithDuration:(NSTimeInterval)duration;
- (void)stopScan;
- (BOOL)connectToDeviceWithAddress:(NSString *)address;
- (BOOL)disconnectFromDeviceWithAddress:(NSString *)address;
- (NSArray<NSString *> *)getServicesForDeviceAddress:(NSString *)address;
- (NSArray<NSString *> *)getCharacteristicsForService:(NSString *)serviceUUID deviceAddress:(NSString *)address;
- (BOOL)readCharacteristic:(NSString *)characteristicUUID serviceUUID:(NSString *)serviceUUID deviceAddress:(NSString *)address;
- (BOOL)writeCharacteristic:(NSString *)characteristicUUID serviceUUID:(NSString *)serviceUUID deviceAddress:(NSString *)address data:(NSData *)data;
- (BOOL)writeCharacteristicWithResponse:(NSString *)characteristicUUID serviceUUID:(NSString *)serviceUUID deviceAddress:(NSString *)address data:(NSData *)data withResponse:(BOOL)withResponse;
- (BOOL)subscribeCharacteristic:(NSString *)characteristicUUID serviceUUID:(NSString *)serviceUUID deviceAddress:(NSString *)address;
- (BOOL)unsubscribeCharacteristic:(NSString *)characteristicUUID serviceUUID:(NSString *)serviceUUID deviceAddress:(NSString *)address;
- (NSInteger)getCharacteristicProperties:(NSString *)characteristicUUID serviceUUID:(NSString *)serviceUUID deviceAddress:(NSString *)address;

// Logging
- (void)unityLog:(NSString *)message;

@end

#endif /* iOSBlePlugin_h */