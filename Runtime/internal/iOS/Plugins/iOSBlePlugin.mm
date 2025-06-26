#import <Foundation/Foundation.h>
#import <CoreBluetooth/CoreBluetooth.h>

// Unity callback delegate types
typedef void (*UnityDeviceDiscoveredCallback)(const char* deviceJson);
typedef void (*UnityDeviceConnectedCallback)(const char* deviceJson);
typedef void (*UnityDeviceDisconnectedCallback)(const char* deviceJson);
typedef void (*UnityScanCompletedCallback)();
typedef void (*UnityCharacteristicValueCallback)(const char* characteristicJson, const char* valueHex);
typedef void (*UnityErrorCallback)(const char* errorMessage);
typedef void (*UnityLogCallback)(const char* logMessage);

@interface iOSBlePlugin : NSObject <CBCentralManagerDelegate, CBPeripheralDelegate>

@property (nonatomic, strong) CBCentralManager *_centralManager;
@property (nonatomic, strong) NSMutableArray<CBPeripheral *> *_discoveredPeripherals;
@property (nonatomic, strong) NSMutableDictionary<NSString *, CBPeripheral *> *_connectedPeripherals;
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

+ (iOSBlePlugin *)sharedInstance;
- (BOOL)initialize;
- (BOOL)startScanWithDuration:(NSTimeInterval)duration;
- (void)stopScan;
- (BOOL)connectToDeviceWithAddress:(NSString *)address;
- (BOOL)disconnectFromDeviceWithAddress:(NSString *)address;
- (NSArray<NSString *> *)getServicesForDeviceAddress:(NSString *)address;
- (NSArray<NSString *> *)getCharacteristicsForService:(NSString *)serviceUUID deviceAddress:(NSString *)address;
- (BOOL)readCharacteristic:(NSString *)characteristicUUID serviceUUID:(NSString *)serviceUUID deviceAddress:(NSString *)address;
- (BOOL)writeCharacteristic:(NSString *)characteristicUUID serviceUUID:(NSString *)serviceUUID deviceAddress:(NSString *)address data:(NSData *)data;

@end

@implementation iOSBlePlugin

+ (iOSBlePlugin *)sharedInstance {
    static iOSBlePlugin *instance = nil;
    static dispatch_once_t onceToken;
    dispatch_once(&onceToken, ^{
        instance = [[iOSBlePlugin alloc] init];
    });
    return instance;
}

- (instancetype)init {
    self = [super init];
    if (self) {
        self._discoveredPeripherals = [[NSMutableArray alloc] init];
        self._connectedPeripherals = [[NSMutableDictionary alloc] init];
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

- (void)onScanTimeout {
    [self unityLog:@"[iOSBlePlugin] Scan timeout reached"];
    [self stopScan];
}

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

- (void)unityLog:(NSString *)message {
    NSLog(@"%@", message);  // Keep NSLog for Xcode console
    if (self.logCallback) {
        self.logCallback([message UTF8String]);
    }
}

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
    
    NSError *error;
    NSData *jsonData = [NSJSONSerialization dataWithJSONObject:deviceDict options:0 error:&error];
    if (error) {
        [self unityLog:[NSString stringWithFormat:@"[iOSBlePlugin] JSON serialization error: %@", error.localizedDescription]];
        return @"{}";
    }
    
    return [[NSString alloc] initWithData:jsonData encoding:NSUTF8StringEncoding];
}

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
}

- (void)peripheral:(CBPeripheral *)peripheral didUpdateValueForCharacteristic:(CBCharacteristic *)characteristic error:(NSError *)error {
    if (error) {
        [self unityLog:[NSString stringWithFormat:@"[iOSBlePlugin] Error reading characteristic: %@", error.localizedDescription]];
        return;
    }
    
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

@end

// C interface for Unity
extern "C" {
    void iOSBlePlugin_SetDeviceDiscoveredCallback(UnityDeviceDiscoveredCallback callback) {
        [iOSBlePlugin sharedInstance].deviceDiscoveredCallback = callback;
    }
    
    void iOSBlePlugin_SetDeviceConnectedCallback(UnityDeviceConnectedCallback callback) {
        [iOSBlePlugin sharedInstance].deviceConnectedCallback = callback;
    }
    
    void iOSBlePlugin_SetDeviceDisconnectedCallback(UnityDeviceDisconnectedCallback callback) {
        [iOSBlePlugin sharedInstance].deviceDisconnectedCallback = callback;
    }
    
    void iOSBlePlugin_SetScanCompletedCallback(UnityScanCompletedCallback callback) {
        [iOSBlePlugin sharedInstance].scanCompletedCallback = callback;
    }
    
    void iOSBlePlugin_SetCharacteristicValueCallback(UnityCharacteristicValueCallback callback) {
        [iOSBlePlugin sharedInstance].characteristicValueCallback = callback;
    }
    
    void iOSBlePlugin_SetErrorCallback(UnityErrorCallback callback) {
        [iOSBlePlugin sharedInstance].errorCallback = callback;
    }
    
    void iOSBlePlugin_SetLogCallback(UnityLogCallback callback) {
        [iOSBlePlugin sharedInstance].logCallback = callback;
    }
    
    BOOL iOSBlePlugin_Initialize() {
        return [[iOSBlePlugin sharedInstance] initialize];
    }
    
    BOOL iOSBlePlugin_IsBluetoothReady() {
        return [iOSBlePlugin sharedInstance]._isBluetoothReady;
    }
    
    BOOL iOSBlePlugin_WaitForBluetoothReady(double timeout) {
        return [[iOSBlePlugin sharedInstance] waitForBluetoothReady:timeout];
    }
    
    BOOL iOSBlePlugin_StartScan(double duration) {
        return [[iOSBlePlugin sharedInstance] startScanWithDuration:duration];
    }
    
    void iOSBlePlugin_StopScan() {
        [[iOSBlePlugin sharedInstance] stopScan];
    }
    
    BOOL iOSBlePlugin_ConnectToDevice(const char* address) {
        NSString *addressStr = [NSString stringWithUTF8String:address];
        return [[iOSBlePlugin sharedInstance] connectToDeviceWithAddress:addressStr];
    }
    
    BOOL iOSBlePlugin_DisconnectFromDevice(const char* address) {
        NSString *addressStr = [NSString stringWithUTF8String:address];
        return [[iOSBlePlugin sharedInstance] disconnectFromDeviceWithAddress:addressStr];
    }
    
    const char** iOSBlePlugin_GetServices(const char* address, int* count) {
        NSString *addressStr = [NSString stringWithUTF8String:address];
        NSArray<NSString *> *services = [[iOSBlePlugin sharedInstance] getServicesForDeviceAddress:addressStr];
        
        *count = (int)services.count;
        if (*count == 0) {
            return NULL;
        }
        
        const char **result = (const char **)malloc(sizeof(char*) * (*count));
        for (int i = 0; i < *count; i++) {
            result[i] = strdup([services[i] UTF8String]);
        }
        
        return result;
    }
    
    const char** iOSBlePlugin_GetCharacteristics(const char* address, const char* serviceUUID, int* count) {
        NSString *addressStr = [NSString stringWithUTF8String:address];
        NSString *serviceUUIDStr = [NSString stringWithUTF8String:serviceUUID];
        NSArray<NSString *> *characteristics = [[iOSBlePlugin sharedInstance] getCharacteristicsForService:serviceUUIDStr deviceAddress:addressStr];
        
        *count = (int)characteristics.count;
        if (*count == 0) {
            return NULL;
        }
        
        const char **result = (const char **)malloc(sizeof(char*) * (*count));
        for (int i = 0; i < *count; i++) {
            result[i] = strdup([characteristics[i] UTF8String]);
        }
        
        return result;
    }
    
    BOOL iOSBlePlugin_ReadCharacteristic(const char* address, const char* serviceUUID, const char* characteristicUUID) {
        NSString *addressStr = [NSString stringWithUTF8String:address];
        NSString *serviceUUIDStr = [NSString stringWithUTF8String:serviceUUID];
        NSString *characteristicUUIDStr = [NSString stringWithUTF8String:characteristicUUID];
        return [[iOSBlePlugin sharedInstance] readCharacteristic:characteristicUUIDStr serviceUUID:serviceUUIDStr deviceAddress:addressStr];
    }
    
    BOOL iOSBlePlugin_WriteCharacteristic(const char* address, const char* serviceUUID, const char* characteristicUUID, const char* dataHex) {
        NSString *addressStr = [NSString stringWithUTF8String:address];
        NSString *serviceUUIDStr = [NSString stringWithUTF8String:serviceUUID];
        NSString *characteristicUUIDStr = [NSString stringWithUTF8String:characteristicUUID];
        NSString *dataHexStr = [NSString stringWithUTF8String:dataHex];
        
        // Convert hex string to NSData
        NSMutableData *data = [[NSMutableData alloc] init];
        for (NSUInteger i = 0; i < dataHexStr.length; i += 2) {
            NSString *hexByte = [dataHexStr substringWithRange:NSMakeRange(i, 2)];
            unsigned char byte = (unsigned char)strtol([hexByte UTF8String], NULL, 16);
            [data appendBytes:&byte length:1];
        }
        
        return [[iOSBlePlugin sharedInstance] writeCharacteristic:characteristicUUIDStr serviceUUID:serviceUUIDStr deviceAddress:addressStr data:data];
    }
}