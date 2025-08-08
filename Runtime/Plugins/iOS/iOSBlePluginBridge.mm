#import "iOSBlePlugin.h"
#import "iOSBlePluginBridge.h"
#import <string.h>

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

    void iOSBlePlugin_SetServicesDiscoveredCallback(UnityServicesDiscoveredCallback callback) {
        [iOSBlePlugin sharedInstance].servicesDiscoveredCallback = callback;
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

    BOOL iOSBlePlugin_WriteCharacteristicWithResponse(const char* address, const char* serviceUUID, const char* characteristicUUID, const char* dataHex, BOOL withResponse) {
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

        return [[iOSBlePlugin sharedInstance] writeCharacteristicWithResponse:characteristicUUIDStr serviceUUID:serviceUUIDStr deviceAddress:addressStr data:data withResponse:withResponse];
    }

    BOOL iOSBlePlugin_SubscribeCharacteristic(const char* address, const char* serviceUUID, const char* characteristicUUID) {
        NSString *addressStr = [NSString stringWithUTF8String:address];
        NSString *serviceUUIDStr = [NSString stringWithUTF8String:serviceUUID];
        NSString *characteristicUUIDStr = [NSString stringWithUTF8String:characteristicUUID];
        return [[iOSBlePlugin sharedInstance] subscribeCharacteristic:characteristicUUIDStr serviceUUID:serviceUUIDStr deviceAddress:addressStr];
    }

    BOOL iOSBlePlugin_UnsubscribeCharacteristic(const char* address, const char* serviceUUID, const char* characteristicUUID) {
        NSString *addressStr = [NSString stringWithUTF8String:address];
        NSString *serviceUUIDStr = [NSString stringWithUTF8String:serviceUUID];
        NSString *characteristicUUIDStr = [NSString stringWithUTF8String:characteristicUUID];
        return [[iOSBlePlugin sharedInstance] unsubscribeCharacteristic:characteristicUUIDStr serviceUUID:serviceUUIDStr deviceAddress:addressStr];
    }

    const char* iOSBlePlugin_GetCharacteristicProperties(const char* address, const char* serviceUUID, const char* characteristicUUID) {
        NSString *addressStr = [NSString stringWithUTF8String:address];
        NSString *serviceUUIDStr = [NSString stringWithUTF8String:serviceUUID];
        NSString *characteristicUUIDStr = [NSString stringWithUTF8String:characteristicUUID];
        NSInteger properties = [[iOSBlePlugin sharedInstance] getCharacteristicProperties:characteristicUUIDStr serviceUUID:serviceUUIDStr deviceAddress:addressStr];

        // Convert integer to string and return as C string
        NSString *propertiesStr = [NSString stringWithFormat:@"%ld", (long)properties];
        return strdup([propertiesStr UTF8String]);
    }
}