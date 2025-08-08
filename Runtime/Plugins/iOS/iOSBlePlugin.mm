#import "iOSBlePlugin.h"
#import "iOSBlePluginPrivate.h"

@implementation iOSBlePlugin

#pragma mark - Singleton

+ (iOSBlePlugin *)sharedInstance {
    static iOSBlePlugin *instance = nil;
    static dispatch_once_t onceToken;
    dispatch_once(&onceToken, ^{
        instance = [[iOSBlePlugin alloc] init];
    });
    return instance;
}

#pragma mark - Initialization

- (instancetype)init {
    self = [super init];
    if (self) {
        self._discoveredPeripherals = [[NSMutableArray alloc] init];
        self._connectedPeripherals = [[NSMutableDictionary alloc] init];
        self._pendingServiceDiscovery = [[NSMutableDictionary alloc] init];
        self._isScanning = NO;
        self._isBluetoothReady = NO;
    }
    return self;
}

- (BOOL)initialize {
    if (self._centralManager != nil) {
        [self unityLog:@"[iOSBlePlugin] Already initialized"];
        return YES;
    }

    // Initialize CBCentralManager with main queue to ensure delegate callbacks work properly
    dispatch_queue_t centralQueue = dispatch_get_main_queue();
    self._centralManager = [[CBCentralManager alloc] initWithDelegate:self queue:centralQueue];
    [self unityLog:@"[iOSBlePlugin] Initializing Core Bluetooth..."];

    // Force NSRunLoop to process events for delegate callback
    [[NSRunLoop currentRunLoop] runMode:NSDefaultRunLoopMode beforeDate:[NSDate dateWithTimeIntervalSinceNow:0.1]];

    // Check if Bluetooth state is immediately available (rare case)
    if (self._centralManager.state == CBManagerStatePoweredOn) {
        self._isBluetoothReady = YES;
        [self unityLog:@"[iOSBlePlugin] Bluetooth immediately ready"];
    }

    return YES;
}

- (BOOL)waitForBluetoothReady:(NSTimeInterval)timeout {
    if (self._isBluetoothReady) {
        return YES;
    }

    [self unityLog:[NSString stringWithFormat:@"[iOSBlePlugin] Waiting for Bluetooth state (timeout: %.1fs)...", timeout]];

    NSDate *startTime = [NSDate date];
    while ([[NSDate date] timeIntervalSinceDate:startTime] < timeout) {
        [[NSRunLoop currentRunLoop] runMode:NSDefaultRunLoopMode beforeDate:[NSDate dateWithTimeIntervalSinceNow:0.1]];

        if (self._isBluetoothReady) {
            [self unityLog:@"[iOSBlePlugin] Bluetooth ready!"];
            return YES;
        }

        if (self._centralManager.state == CBManagerStatePoweredOff ||
            self._centralManager.state == CBManagerStateUnsupported ||
            self._centralManager.state == CBManagerStateUnauthorized) {
            [self unityLog:[NSString stringWithFormat:@"[iOSBlePlugin] Bluetooth not available (state: %ld)", (long)self._centralManager.state]];
            return NO;
        }
    }

    [self unityLog:@"[iOSBlePlugin] Timeout waiting for Bluetooth state"];
    return NO;
}

#pragma mark - Scanning

- (BOOL)startScanWithDuration:(NSTimeInterval)duration {
    if (self._centralManager.state == CBManagerStateUnknown) {
        [self unityLog:@"[iOSBlePlugin] Bluetooth state is unknown, waiting for initialization..."];
        if (self.errorCallback) {
            self.errorCallback("Bluetooth state is unknown, please try again in a moment");
        }
        return NO;
    }

    if (self._centralManager.state != CBManagerStatePoweredOn) {
        [self unityLog:[NSString stringWithFormat:@"[iOSBlePlugin] Bluetooth is not powered on, current state: %ld", (long)self._centralManager.state]];
        if (self.errorCallback) {
            self.errorCallback("Bluetooth is not powered on");
        }
        return NO;
    }

    if (self._isScanning) {
        [self unityLog:@"[iOSBlePlugin] Already scanning"];
        return NO;
    }

    [self._discoveredPeripherals removeAllObjects];

    [self unityLog:[NSString stringWithFormat:@"[iOSBlePlugin] Starting BLE scan for %.1f seconds", duration]];
    [self._centralManager scanForPeripheralsWithServices:nil options:@{CBCentralManagerScanOptionAllowDuplicatesKey: @NO}];
    self._isScanning = YES;

    // Set timer to stop scan after duration
    self._scanTimer = [NSTimer scheduledTimerWithTimeInterval:duration
                                                      target:self
                                                    selector:@selector(onScanTimeout)
                                                    userInfo:nil
                                                     repeats:NO];

    return YES;
}

- (void)stopScan {
    if (!self._isScanning) {
        [self unityLog:@"[iOSBlePlugin] Not currently scanning"];
        return;
    }

    [self._centralManager stopScan];
    self._isScanning = NO;

    if (self._scanTimer) {
        [self._scanTimer invalidate];
        self._scanTimer = nil;
    }

    [self unityLog:@"[iOSBlePlugin] BLE scan stopped"];

    if (self.scanCompletedCallback) {
        self.scanCompletedCallback();
    }
}

#pragma mark - Connection Management

- (BOOL)connectToDeviceWithAddress:(NSString *)address {
    CBPeripheral *peripheral = [self findPeripheralByAddress:address];
    if (!peripheral) {
        [self unityLog:[NSString stringWithFormat:@"[iOSBlePlugin] Peripheral not found for address: %@", address]];
        if (self.errorCallback) {
            self.errorCallback([[NSString stringWithFormat:@"Peripheral not found: %@", address] UTF8String]);
        }
        return NO;
    }

    [self unityLog:[NSString stringWithFormat:@"[iOSBlePlugin] Connecting to device: %@", address]];
    [self._centralManager connectPeripheral:peripheral options:nil];
    return YES;
}

- (BOOL)disconnectFromDeviceWithAddress:(NSString *)address {
    CBPeripheral *peripheral = self._connectedPeripherals[address];
    if (!peripheral) {
        [self unityLog:[NSString stringWithFormat:@"[iOSBlePlugin] No connected peripheral found for address: %@", address]];
        return NO;
    }

    [self unityLog:[NSString stringWithFormat:@"[iOSBlePlugin] Disconnecting from device: %@", address]];
    [self._centralManager cancelPeripheralConnection:peripheral];
    return YES;
}

#pragma mark - Service and Characteristic Discovery

- (NSArray<NSString *> *)getServicesForDeviceAddress:(NSString *)address {
    CBPeripheral *peripheral = self._connectedPeripherals[address];
    if (!peripheral || !peripheral.services) {
        return @[];
    }

    NSMutableArray<NSString *> *serviceUUIDs = [[NSMutableArray alloc] init];
    for (CBService *service in peripheral.services) {
        [serviceUUIDs addObject:service.UUID.UUIDString];
    }

    return [serviceUUIDs copy];
}

- (NSArray<NSString *> *)getCharacteristicsForService:(NSString *)serviceUUID deviceAddress:(NSString *)address {
    CBPeripheral *peripheral = self._connectedPeripherals[address];
    if (!peripheral) {
        return @[];
    }

    CBService *targetService = nil;
    for (CBService *service in peripheral.services) {
        if ([service.UUID.UUIDString isEqualToString:serviceUUID]) {
            targetService = service;
            break;
        }
    }

    if (!targetService || !targetService.characteristics) {
        return @[];
    }

    NSMutableArray<NSString *> *characteristicUUIDs = [[NSMutableArray alloc] init];
    for (CBCharacteristic *characteristic in targetService.characteristics) {
        [characteristicUUIDs addObject:characteristic.UUID.UUIDString];
    }

    return [characteristicUUIDs copy];
}

#pragma mark - Characteristic Operations

- (BOOL)readCharacteristic:(NSString *)characteristicUUID serviceUUID:(NSString *)serviceUUID deviceAddress:(NSString *)address {
    CBCharacteristic *characteristic = [self findCharacteristic:characteristicUUID inService:serviceUUID forDevice:address];
    if (!characteristic) {
        return NO;
    }

    CBPeripheral *peripheral = self._connectedPeripherals[address];
    [peripheral readValueForCharacteristic:characteristic];
    return YES;
}

- (BOOL)writeCharacteristic:(NSString *)characteristicUUID serviceUUID:(NSString *)serviceUUID deviceAddress:(NSString *)address data:(NSData *)data {
    CBCharacteristic *characteristic = [self findCharacteristic:characteristicUUID inService:serviceUUID forDevice:address];
    if (!characteristic) {
        return NO;
    }

    CBPeripheral *peripheral = self._connectedPeripherals[address];
    [peripheral writeValue:data forCharacteristic:characteristic type:CBCharacteristicWriteWithResponse];
    return YES;
}

- (BOOL)writeCharacteristicWithResponse:(NSString *)characteristicUUID serviceUUID:(NSString *)serviceUUID deviceAddress:(NSString *)address data:(NSData *)data withResponse:(BOOL)withResponse {
    CBCharacteristic *characteristic = [self findCharacteristic:characteristicUUID inService:serviceUUID forDevice:address];
    if (!characteristic) {
        [self unityLog:[NSString stringWithFormat:@"[iOSBlePlugin] Characteristic not found: %@ in service: %@ for device: %@", characteristicUUID, serviceUUID, address]];
        return NO;
    }

    CBPeripheral *peripheral = self._connectedPeripherals[address];
    CBCharacteristicWriteType writeType = withResponse ? CBCharacteristicWriteWithResponse : CBCharacteristicWriteWithoutResponse;

    [self unityLog:[NSString stringWithFormat:@"[iOSBlePlugin] Writing to characteristic %@ with response: %@", characteristicUUID, withResponse ? @"YES" : @"NO"]];
    [peripheral writeValue:data forCharacteristic:characteristic type:writeType];
    return YES;
}

- (BOOL)subscribeCharacteristic:(NSString *)characteristicUUID serviceUUID:(NSString *)serviceUUID deviceAddress:(NSString *)address {
    CBCharacteristic *characteristic = [self findCharacteristic:characteristicUUID inService:serviceUUID forDevice:address];
    if (!characteristic) {
        [self unityLog:[NSString stringWithFormat:@"[iOSBlePlugin] Characteristic not found for subscription: %@ in service: %@ for device: %@", characteristicUUID, serviceUUID, address]];
        return NO;
    }

    // Check if characteristic supports notifications or indications
    if (!(characteristic.properties & CBCharacteristicPropertyNotify) && !(characteristic.properties & CBCharacteristicPropertyIndicate)) {
        [self unityLog:[NSString stringWithFormat:@"[iOSBlePlugin] Characteristic %@ does not support notifications or indications", characteristicUUID]];
        return NO;
    }

    CBPeripheral *peripheral = self._connectedPeripherals[address];
    [self unityLog:[NSString stringWithFormat:@"[iOSBlePlugin] Subscribing to characteristic: %@", characteristicUUID]];
    [peripheral setNotifyValue:YES forCharacteristic:characteristic];
    return YES;
}

- (BOOL)unsubscribeCharacteristic:(NSString *)characteristicUUID serviceUUID:(NSString *)serviceUUID deviceAddress:(NSString *)address {
    CBCharacteristic *characteristic = [self findCharacteristic:characteristicUUID inService:serviceUUID forDevice:address];
    if (!characteristic) {
        [self unityLog:[NSString stringWithFormat:@"[iOSBlePlugin] Characteristic not found for unsubscription: %@ in service: %@ for device: %@", characteristicUUID, serviceUUID, address]];
        return NO;
    }

    CBPeripheral *peripheral = self._connectedPeripherals[address];
    [self unityLog:[NSString stringWithFormat:@"[iOSBlePlugin] Unsubscribing from characteristic: %@", characteristicUUID]];
    [peripheral setNotifyValue:NO forCharacteristic:characteristic];
    return YES;
}

- (NSInteger)getCharacteristicProperties:(NSString *)characteristicUUID serviceUUID:(NSString *)serviceUUID deviceAddress:(NSString *)address {
    CBCharacteristic *characteristic = [self findCharacteristic:characteristicUUID inService:serviceUUID forDevice:address];
    if (!characteristic) {
        [self unityLog:[NSString stringWithFormat:@"[iOSBlePlugin] Characteristic not found for properties: %@ in service: %@ for device: %@", characteristicUUID, serviceUUID, address]];
        return 0;
    }

    // Convert CBCharacteristicProperties to our custom enum values
    NSInteger properties = 0;

    if (characteristic.properties & CBCharacteristicPropertyBroadcast) {
        properties |= 0x01; // Broadcast
    }
    if (characteristic.properties & CBCharacteristicPropertyRead) {
        properties |= 0x02; // Read
    }
    if (characteristic.properties & CBCharacteristicPropertyWriteWithoutResponse) {
        properties |= 0x04; // WriteWithoutResponse
    }
    if (characteristic.properties & CBCharacteristicPropertyWrite) {
        properties |= 0x08; // Write
    }
    if (characteristic.properties & CBCharacteristicPropertyNotify) {
        properties |= 0x10; // Notify
    }
    if (characteristic.properties & CBCharacteristicPropertyIndicate) {
        properties |= 0x20; // Indicate
    }
    if (characteristic.properties & CBCharacteristicPropertyAuthenticatedSignedWrites) {
        properties |= 0x40; // AuthenticatedSignedWrites
    }
    if (characteristic.properties & CBCharacteristicPropertyExtendedProperties) {
        properties |= 0x80; // ExtendedProperties
    }

    [self unityLog:[NSString stringWithFormat:@"[iOSBlePlugin] Characteristic %@ properties: %ld", characteristicUUID, (long)properties]];
    return properties;
}

#pragma mark - Logging

- (void)unityLog:(NSString *)message {
    NSLog(@"%@", message);  // Keep NSLog for Xcode console
    if (self.logCallback) {
        self.logCallback([message UTF8String]);
    }
}

@end