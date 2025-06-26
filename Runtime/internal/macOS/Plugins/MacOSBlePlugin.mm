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

@interface MacOSBlePlugin : NSObject <CBCentralManagerDelegate, CBPeripheralDelegate>

@property (nonatomic, strong) CBCentralManager *centralManager;
@property (nonatomic, strong) NSMutableArray<CBPeripheral *> *discoveredPeripherals;
@property (nonatomic, strong) NSMutableDictionary<NSString *, CBPeripheral *> *connectedPeripherals;
@property (nonatomic, assign) BOOL isScanning;
@property (nonatomic, strong) NSTimer *scanTimer;
@property (nonatomic, assign) BOOL isBluetoothReady;

// Unity callbacks
@property (nonatomic, assign) UnityDeviceDiscoveredCallback deviceDiscoveredCallback;
@property (nonatomic, assign) UnityDeviceConnectedCallback deviceConnectedCallback;
@property (nonatomic, assign) UnityDeviceDisconnectedCallback deviceDisconnectedCallback;
@property (nonatomic, assign) UnityScanCompletedCallback scanCompletedCallback;
@property (nonatomic, assign) UnityCharacteristicValueCallback characteristicValueCallback;
@property (nonatomic, assign) UnityErrorCallback errorCallback;
@property (nonatomic, assign) UnityLogCallback logCallback;

+ (MacOSBlePlugin *)sharedInstance;
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

@implementation MacOSBlePlugin

+ (MacOSBlePlugin *)sharedInstance {
    static MacOSBlePlugin *instance = nil;
    static dispatch_once_t onceToken;
    dispatch_once(&onceToken, ^{
        instance = [[MacOSBlePlugin alloc] init];
    });
    return instance;
}

- (instancetype)init {
    self = [super init];
    if (self) {
        self.discoveredPeripherals = [[NSMutableArray alloc] init];
        self.connectedPeripherals = [[NSMutableDictionary alloc] init];
        self.isScanning = NO;
        self.isBluetoothReady = NO;
    }
    return self;
}

- (BOOL)initialize {
    if (self.centralManager != nil) {
        [self unityLog:@"[MacOSBlePlugin] Already initialized"];
        return YES;
    }

    self.centralManager = [[CBCentralManager alloc] initWithDelegate:self queue:nil];
    [self unityLog:@"[MacOSBlePlugin] Initializing Core Bluetooth..."];

    return YES;
}

- (BOOL)waitForBluetoothReady:(NSTimeInterval)timeout {
    if (self.isBluetoothReady) {
        return YES;
    }

    [self unityLog:[NSString stringWithFormat:@"[MacOSBlePlugin] Waiting for Bluetooth state (timeout: %.1fs)...", timeout]];

    NSDate *startTime = [NSDate date];
    while ([[NSDate date] timeIntervalSinceDate:startTime] < timeout) {
        [[NSRunLoop currentRunLoop] runMode:NSDefaultRunLoopMode beforeDate:[NSDate dateWithTimeIntervalSinceNow:0.1]];

        if (self.isBluetoothReady) {
            [self unityLog:@"[MacOSBlePlugin] Bluetooth ready!"];
            return YES;
        }

        if (self.centralManager.state == CBManagerStatePoweredOff ||
            self.centralManager.state == CBManagerStateUnsupported ||
            self.centralManager.state == CBManagerStateUnauthorized) {
            [self unityLog:[NSString stringWithFormat:@"[MacOSBlePlugin] Bluetooth not available (state: %ld)", (long)self.centralManager.state]];
            return NO;
        }
    }

    [self unityLog:@"[MacOSBlePlugin] Timeout waiting for Bluetooth state"];
    return NO;
}

- (BOOL)startScanWithDuration:(NSTimeInterval)duration {
    if (self.centralManager.state == CBManagerStateUnknown) {
        [self unityLog:@"[MacOSBlePlugin] Bluetooth state is unknown, waiting for initialization..."];
        if (self.errorCallback) {
            self.errorCallback("Bluetooth state is unknown, please try again in a moment");
        }
        return NO;
    }

    if (self.centralManager.state != CBManagerStatePoweredOn) {
        [self unityLog:[NSString stringWithFormat:@"[MacOSBlePlugin] Bluetooth is not powered on, current state: %ld", (long)self.centralManager.state]];
        if (self.errorCallback) {
            self.errorCallback("Bluetooth is not powered on");
        }
        return NO;
    }

    if (self.isScanning) {
        [self unityLog:@"[MacOSBlePlugin] Already scanning"];
        return NO;
    }

    [self.discoveredPeripherals removeAllObjects];

    [self unityLog:[NSString stringWithFormat:@"[MacOSBlePlugin] Starting BLE scan for %.1f seconds", duration]];
    [self.centralManager scanForPeripheralsWithServices:nil options:@{CBCentralManagerScanOptionAllowDuplicatesKey: @NO}];
    self.isScanning = YES;

    // Set timer to stop scan after duration
    self.scanTimer = [NSTimer scheduledTimerWithTimeInterval:duration
                                                      target:self
                                                    selector:@selector(onScanTimeout)
                                                    userInfo:nil
                                                     repeats:NO];

    return YES;
}

- (void)stopScan {
    if (!self.isScanning) {
        [self unityLog:@"[MacOSBlePlugin] Not currently scanning"];
        return;
    }

    [self.centralManager stopScan];
    self.isScanning = NO;

    if (self.scanTimer) {
        [self.scanTimer invalidate];
        self.scanTimer = nil;
    }

    [self unityLog:@"[MacOSBlePlugin] BLE scan stopped"];

    if (self.scanCompletedCallback) {
        self.scanCompletedCallback();
    }
}

- (void)onScanTimeout {
    [self unityLog:@"[MacOSBlePlugin] Scan timeout reached"];
    [self stopScan];
}

- (BOOL)connectToDeviceWithAddress:(NSString *)address {
    CBPeripheral *peripheral = [self findPeripheralByAddress:address];
    if (!peripheral) {
        [self unityLog:[NSString stringWithFormat:@"[MacOSBlePlugin] Peripheral not found for address: %@", address]];
        if (self.errorCallback) {
            self.errorCallback([[NSString stringWithFormat:@"Peripheral not found: %@", address] UTF8String]);
        }
        return NO;
    }

    [self unityLog:[NSString stringWithFormat:@"[MacOSBlePlugin] Connecting to device: %@", address]];
    [self.centralManager connectPeripheral:peripheral options:nil];
    return YES;
}

- (BOOL)disconnectFromDeviceWithAddress:(NSString *)address {
    CBPeripheral *peripheral = self.connectedPeripherals[address];
    if (!peripheral) {
        [self unityLog:[NSString stringWithFormat:@"[MacOSBlePlugin] No connected peripheral found for address: %@", address]];
        return NO;
    }

    [self unityLog:[NSString stringWithFormat:@"[MacOSBlePlugin] Disconnecting from device: %@", address]];
    [self.centralManager cancelPeripheralConnection:peripheral];
    return YES;
}

- (NSArray<NSString *> *)getServicesForDeviceAddress:(NSString *)address {
    CBPeripheral *peripheral = self.connectedPeripherals[address];
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
    CBPeripheral *peripheral = self.connectedPeripherals[address];
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

    CBPeripheral *peripheral = self.connectedPeripherals[address];
    [peripheral readValueForCharacteristic:characteristic];
    return YES;
}

- (BOOL)writeCharacteristic:(NSString *)characteristicUUID serviceUUID:(NSString *)serviceUUID deviceAddress:(NSString *)address data:(NSData *)data {
    CBCharacteristic *characteristic = [self findCharacteristic:characteristicUUID inService:serviceUUID forDevice:address];
    if (!characteristic) {
        return NO;
    }

    CBPeripheral *peripheral = self.connectedPeripherals[address];
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
    for (CBPeripheral *peripheral in self.discoveredPeripherals) {
        if ([peripheral.identifier.UUIDString isEqualToString:address]) {
            return peripheral;
        }
    }
    return nil;
}

- (CBCharacteristic *)findCharacteristic:(NSString *)characteristicUUID inService:(NSString *)serviceUUID forDevice:(NSString *)address {
    CBPeripheral *peripheral = self.connectedPeripherals[address];
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
    @try {
        NSMutableDictionary *deviceDict = [[NSMutableDictionary alloc] init];

        // Safely handle peripheral name
        NSString *deviceName = @"Unknown Device";
        if (peripheral.name && [peripheral.name isKindOfClass:[NSString class]] && peripheral.name.length > 0) {
            deviceName = peripheral.name;
        }
        deviceDict[@"name"] = deviceName;
        
        // Safely handle peripheral address
        NSString *deviceAddress = @"00:00:00:00:00:00";
        if (peripheral.identifier && peripheral.identifier.UUIDString) {
            deviceAddress = peripheral.identifier.UUIDString;
        }
        deviceDict[@"address"] = deviceAddress;
        
        // Safely handle rssi parameter
        NSNumber *deviceRssi = @(-50);
        if (rssi && [rssi isKindOfClass:[NSNumber class]]) {
            deviceRssi = rssi;
        }
        deviceDict[@"rssi"] = deviceRssi;
        
        deviceDict[@"isConnectable"] = @YES;
        deviceDict[@"txPower"] = @0;
        deviceDict[@"advertisingData"] = @"";

        NSError *error = nil;
        NSData *jsonData = [NSJSONSerialization dataWithJSONObject:deviceDict options:0 error:&error];
        if (error) {
            [self unityLog:[NSString stringWithFormat:@"[MacOSBlePlugin] JSON serialization error: %@", error.localizedDescription]];
            return @"{\"name\":\"Unknown Device\",\"address\":\"00:00:00:00:00:00\",\"rssi\":-50,\"isConnectable\":true,\"txPower\":0,\"advertisingData\":\"\"}";
        }

        NSString *jsonString = [[NSString alloc] initWithData:jsonData encoding:NSUTF8StringEncoding];
        return jsonString ?: @"{\"name\":\"Unknown Device\",\"address\":\"00:00:00:00:00:00\",\"rssi\":-50,\"isConnectable\":true,\"txPower\":0,\"advertisingData\":\"\"}";
    }
    @catch (NSException *exception) {
        [self unityLog:[NSString stringWithFormat:@"[MacOSBlePlugin] Exception in peripheralToJSON: %@", exception.reason]];
        return @"{\"name\":\"Unknown Device\",\"address\":\"00:00:00:00:00:00\",\"rssi\":-50,\"isConnectable\":true,\"txPower\":0,\"advertisingData\":\"\"}";
    }
}

#pragma mark - CBCentralManagerDelegate

- (void)centralManagerDidUpdateState:(CBCentralManager *)central {
    switch (central.state) {
        case CBManagerStatePoweredOn:
            [self unityLog:@"[MacOSBlePlugin] Bluetooth powered on"];
            self.isBluetoothReady = YES;
            break;
        case CBManagerStatePoweredOff:
            [self unityLog:@"[MacOSBlePlugin] Bluetooth powered off"];
            self.isBluetoothReady = NO;
            break;
        case CBManagerStateUnsupported:
            [self unityLog:@"[MacOSBlePlugin] Bluetooth unsupported"];
            self.isBluetoothReady = NO;
            break;
        case CBManagerStateUnauthorized:
            [self unityLog:@"[MacOSBlePlugin] Bluetooth unauthorized"];
            self.isBluetoothReady = NO;
            break;
        default:
            [self unityLog:@"[MacOSBlePlugin] Bluetooth state unknown"];
            self.isBluetoothReady = NO;
            break;
    }
}

- (void)centralManager:(CBCentralManager *)central didDiscoverPeripheral:(CBPeripheral *)peripheral advertisementData:(NSDictionary<NSString *,id> *)advertisementData RSSI:(NSNumber *)RSSI {
    @try {
        // Validate input parameters
        if (!peripheral || !peripheral.identifier) {
            [self unityLog:@"[MacOSBlePlugin] Invalid peripheral in didDiscoverPeripheral"];
            return;
        }

        // Avoid duplicates
        for (CBPeripheral *existing in self.discoveredPeripherals) {
            if ([existing.identifier.UUIDString isEqualToString:peripheral.identifier.UUIDString]) {
                return;
            }
        }

        [self.discoveredPeripherals addObject:peripheral];

        // Safe logging
        NSString *peripheralName = (peripheral.name && peripheral.name.length > 0) ? peripheral.name : @"Unknown";
        NSString *rssiString = (RSSI && [RSSI isKindOfClass:[NSNumber class]]) ? [RSSI stringValue] : @"Unknown";
        
        [self unityLog:[NSString stringWithFormat:@"[MacOSBlePlugin] Discovered peripheral: %@ (%@) RSSI: %@",
              peripheralName, peripheral.identifier.UUIDString, rssiString]];

        if (self.deviceDiscoveredCallback) {
            NSString *deviceJson = [self peripheralToJSON:peripheral rssi:RSSI];
            if (deviceJson && deviceJson.length > 0) {
                self.deviceDiscoveredCallback([deviceJson UTF8String]);
            }
        }
    }
    @catch (NSException *exception) {
        [self unityLog:[NSString stringWithFormat:@"[MacOSBlePlugin] Exception in didDiscoverPeripheral: %@", exception.reason]];
    }
}

- (void)centralManager:(CBCentralManager *)central didConnectPeripheral:(CBPeripheral *)peripheral {
    [self unityLog:[NSString stringWithFormat:@"[MacOSBlePlugin] Connected to peripheral: %@", peripheral.identifier.UUIDString]];

    self.connectedPeripherals[peripheral.identifier.UUIDString] = peripheral;
    peripheral.delegate = self;

    // Discover services
    [peripheral discoverServices:nil];

    if (self.deviceConnectedCallback) {
        NSString *deviceJson = [self peripheralToJSON:peripheral rssi:nil];
        self.deviceConnectedCallback([deviceJson UTF8String]);
    }
}

- (void)centralManager:(CBCentralManager *)central didDisconnectPeripheral:(CBPeripheral *)peripheral error:(NSError *)error {
    [self unityLog:[NSString stringWithFormat:@"[MacOSBlePlugin] Disconnected from peripheral: %@", peripheral.identifier.UUIDString]];

    [self.connectedPeripherals removeObjectForKey:peripheral.identifier.UUIDString];

    if (self.deviceDisconnectedCallback) {
        NSString *deviceJson = [self peripheralToJSON:peripheral rssi:nil];
        self.deviceDisconnectedCallback([deviceJson UTF8String]);
    }
}

- (void)centralManager:(CBCentralManager *)central didFailToConnectPeripheral:(CBPeripheral *)peripheral error:(NSError *)error {
    [self unityLog:[NSString stringWithFormat:@"[MacOSBlePlugin] Failed to connect to peripheral: %@ Error: %@", peripheral.identifier.UUIDString, error.localizedDescription]];

    if (self.errorCallback) {
        NSString *errorMsg = [NSString stringWithFormat:@"Failed to connect to %@: %@", peripheral.identifier.UUIDString, error.localizedDescription];
        self.errorCallback([errorMsg UTF8String]);
    }
}

#pragma mark - CBPeripheralDelegate

- (void)peripheral:(CBPeripheral *)peripheral didDiscoverServices:(NSError *)error {
    if (error) {
        [self unityLog:[NSString stringWithFormat:@"[MacOSBlePlugin] Error discovering services: %@", error.localizedDescription]];
        return;
    }

    [self unityLog:[NSString stringWithFormat:@"[MacOSBlePlugin] Discovered %lu services for peripheral: %@", (unsigned long)peripheral.services.count, peripheral.identifier.UUIDString]];

    // Discover characteristics for all services
    for (CBService *service in peripheral.services) {
        [peripheral discoverCharacteristics:nil forService:service];
    }
}

- (void)peripheral:(CBPeripheral *)peripheral didDiscoverCharacteristicsForService:(CBService *)service error:(NSError *)error {
    if (error) {
        [self unityLog:[NSString stringWithFormat:@"[MacOSBlePlugin] Error discovering characteristics: %@", error.localizedDescription]];
        return;
    }

    [self unityLog:[NSString stringWithFormat:@"[MacOSBlePlugin] Discovered %lu characteristics for service: %@", (unsigned long)service.characteristics.count, service.UUID.UUIDString]];
}

- (void)peripheral:(CBPeripheral *)peripheral didUpdateValueForCharacteristic:(CBCharacteristic *)characteristic error:(NSError *)error {
    if (error) {
        [self unityLog:[NSString stringWithFormat:@"[MacOSBlePlugin] Error reading characteristic: %@", error.localizedDescription]];
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
    void MacOSBlePlugin_SetDeviceDiscoveredCallback(UnityDeviceDiscoveredCallback callback) {
        [MacOSBlePlugin sharedInstance].deviceDiscoveredCallback = callback;
    }

    void MacOSBlePlugin_SetDeviceConnectedCallback(UnityDeviceConnectedCallback callback) {
        [MacOSBlePlugin sharedInstance].deviceConnectedCallback = callback;
    }

    void MacOSBlePlugin_SetDeviceDisconnectedCallback(UnityDeviceDisconnectedCallback callback) {
        [MacOSBlePlugin sharedInstance].deviceDisconnectedCallback = callback;
    }

    void MacOSBlePlugin_SetScanCompletedCallback(UnityScanCompletedCallback callback) {
        [MacOSBlePlugin sharedInstance].scanCompletedCallback = callback;
    }

    void MacOSBlePlugin_SetCharacteristicValueCallback(UnityCharacteristicValueCallback callback) {
        [MacOSBlePlugin sharedInstance].characteristicValueCallback = callback;
    }

    void MacOSBlePlugin_SetErrorCallback(UnityErrorCallback callback) {
        [MacOSBlePlugin sharedInstance].errorCallback = callback;
    }

    void MacOSBlePlugin_SetLogCallback(UnityLogCallback callback) {
        [MacOSBlePlugin sharedInstance].logCallback = callback;
    }

    BOOL MacOSBlePlugin_Initialize() {
        return [[MacOSBlePlugin sharedInstance] initialize];
    }

    BOOL MacOSBlePlugin_IsBluetoothReady() {
        return [MacOSBlePlugin sharedInstance].isBluetoothReady;
    }

    BOOL MacOSBlePlugin_WaitForBluetoothReady(double timeout) {
        return [[MacOSBlePlugin sharedInstance] waitForBluetoothReady:timeout];
    }

    BOOL MacOSBlePlugin_StartScan(double duration) {
        return [[MacOSBlePlugin sharedInstance] startScanWithDuration:duration];
    }

    void MacOSBlePlugin_StopScan() {
        [[MacOSBlePlugin sharedInstance] stopScan];
    }

    BOOL MacOSBlePlugin_ConnectToDevice(const char* address) {
        NSString *addressStr = [NSString stringWithUTF8String:address];
        return [[MacOSBlePlugin sharedInstance] connectToDeviceWithAddress:addressStr];
    }

    BOOL MacOSBlePlugin_DisconnectFromDevice(const char* address) {
        NSString *addressStr = [NSString stringWithUTF8String:address];
        return [[MacOSBlePlugin sharedInstance] disconnectFromDeviceWithAddress:addressStr];
    }

    const char** MacOSBlePlugin_GetServices(const char* address, int* count) {
        NSString *addressStr = [NSString stringWithUTF8String:address];
        NSArray<NSString *> *services = [[MacOSBlePlugin sharedInstance] getServicesForDeviceAddress:addressStr];

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

    const char** MacOSBlePlugin_GetCharacteristics(const char* address, const char* serviceUUID, int* count) {
        NSString *addressStr = [NSString stringWithUTF8String:address];
        NSString *serviceUUIDStr = [NSString stringWithUTF8String:serviceUUID];
        NSArray<NSString *> *characteristics = [[MacOSBlePlugin sharedInstance] getCharacteristicsForService:serviceUUIDStr deviceAddress:addressStr];

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

    BOOL MacOSBlePlugin_ReadCharacteristic(const char* address, const char* serviceUUID, const char* characteristicUUID) {
        NSString *addressStr = [NSString stringWithUTF8String:address];
        NSString *serviceUUIDStr = [NSString stringWithUTF8String:serviceUUID];
        NSString *characteristicUUIDStr = [NSString stringWithUTF8String:characteristicUUID];
        return [[MacOSBlePlugin sharedInstance] readCharacteristic:characteristicUUIDStr serviceUUID:serviceUUIDStr deviceAddress:addressStr];
    }

    BOOL MacOSBlePlugin_WriteCharacteristic(const char* address, const char* serviceUUID, const char* characteristicUUID, const char* dataHex) {
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

        return [[MacOSBlePlugin sharedInstance] writeCharacteristic:characteristicUUIDStr serviceUUID:serviceUUIDStr deviceAddress:addressStr data:data];
    }
}