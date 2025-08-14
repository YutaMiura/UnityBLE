#import <CoreBluetooth/CoreBluetooth.h>
#import <Foundation/Foundation.h>

#import "UnityBlePlugin-Swift.h"
#import "UnityBridge.h"

extern "C" {
int UnityBLE_StartScanning(const char **serviceUUIDs, const char *nameFilter) {
  NSArray<CBUUID *> *uuids = nil;
  if (serviceUUIDs != NULL) {
    NSMutableArray<CBUUID *> *uuidArray = [NSMutableArray array];
    for (int i = 0; serviceUUIDs[i] != NULL; i++) {
      NSString *uuidString = [NSString stringWithUTF8String:serviceUUIDs[i]];
      CBUUID *uuid = [CBUUID UUIDWithString:uuidString];
      [uuidArray addObject:uuid];
    }
    uuids = [uuidArray copy];
  }

  NSString *nameFilterString = nil;
  if (nameFilter != NULL) {
    nameFilterString = [NSString stringWithUTF8String:nameFilter];
  }

  return (int)[[UnityBridgeFacade shared] startScanningFor:uuids
                                                nameFilter:nameFilterString];
}

void UnityBLE_StopScanning() {
  return [[UnityBridgeFacade shared] stopScanning];
}

bool UnityBLE_IsScanning() { return [[UnityBridgeFacade shared] isScanning]; }

int UnityBLE_ConnectToPeripheral(const char *peripheralUUID) {
  NSString *uuidString = [NSString stringWithUTF8String:peripheralUUID];
  NSUUID *uuid = [[NSUUID alloc] initWithUUIDString:uuidString];
  return (int)[[UnityBridgeFacade shared] connectToPeripheralWithUUID:uuid];
}

int UnityBLE_DisconnectFromPeripheral(const char *peripheralUUID) {
  NSString *uuidString = [NSString stringWithUTF8String:peripheralUUID];
  NSUUID *uuid = [[NSUUID alloc] initWithUUIDString:uuidString];
  return (int)[[UnityBridgeFacade shared]
      disconnectFromPeripheralWithUUID:uuid];
}

int UnityBLE_discoverServices(const char *peripheralUUID) {
  NSString *uuidString = [NSString stringWithUTF8String:peripheralUUID];
  NSUUID *uuid = [[NSUUID alloc] initWithUUIDString:uuidString];
  return (int)[[UnityBridgeFacade shared] discoverServicesForPeripheral:uuid];
}

int UnityBLE_ReadCharacteristic(const char *peripheralUUID,
                                const char *serviceUUID,
                                const char *characteristicUUID) {
  NSString *peripheralUuidString =
      [NSString stringWithUTF8String:peripheralUUID];
  NSString *serviceUuidString = [NSString stringWithUTF8String:serviceUUID];
  NSString *characteristicUuidString =
      [NSString stringWithUTF8String:characteristicUUID];
  NSUUID *peripheralUuid =
      [[NSUUID alloc] initWithUUIDString:peripheralUuidString];
  CBUUID *serviceUuid = [CBUUID UUIDWithString:serviceUuidString];
  CBUUID *characteristicUuid = [CBUUID UUIDWithString:characteristicUuidString];

  return (int)[[UnityBridgeFacade shared]
      readCharacteristicWithUUID:characteristicUuid
                      forService:serviceUuid
                    ofPeripheral:peripheralUuid];
}

int UnityBLE_WriteCharacteristic(const char *peripheralUUID,
                                 const char *serviceUUID,
                                 const char *characteristicUUID,
                                 const char *data) {
  NSString *peripheralUuidString =
      [NSString stringWithUTF8String:peripheralUUID];
  NSString *serviceUuidString = [NSString stringWithUTF8String:serviceUUID];
  NSString *characteristicUuidString =
      [NSString stringWithUTF8String:characteristicUUID];
  NSUUID *peripheralUuid =
      [[NSUUID alloc] initWithUUIDString:peripheralUuidString];
  CBUUID *serviceUuid = [CBUUID UUIDWithString:serviceUuidString];
  CBUUID *characteristicUuid = [CBUUID UUIDWithString:characteristicUuidString];

  NSString *dataHexStr = [NSString stringWithUTF8String:data];

  // Convert hex string to NSData
  NSMutableData *d = [[NSMutableData alloc] init];
  for (NSUInteger i = 0; i < dataHexStr.length; i += 2) {
    NSString *hexByte = [dataHexStr substringWithRange:NSMakeRange(i, 2)];
    unsigned char byte = (unsigned char)strtol([hexByte UTF8String], NULL, 16);
    [d appendBytes:&byte length:1];
  }

  return (int)[[UnityBridgeFacade shared] writeValue:d
                                    toCharacteristic:characteristicUuid
                                          forService:serviceUuid
                                        ofPeripheral:peripheralUuid];
}

int UnityBLE_SubscribeToCharacteristic(const char *peripheralUUID,
                                       const char *serviceUUID,
                                       const char *characteristicUUID) {
  NSString *peripheralUuidString =
      [NSString stringWithUTF8String:peripheralUUID];
  NSString *serviceUuidString = [NSString stringWithUTF8String:serviceUUID];
  NSString *characteristicUuidString =
      [NSString stringWithUTF8String:characteristicUUID];
  NSUUID *peripheralUuid =
      [[NSUUID alloc] initWithUUIDString:peripheralUuidString];
  CBUUID *serviceUuid = [CBUUID UUIDWithString:serviceUuidString];
  CBUUID *characteristicUuid = [CBUUID UUIDWithString:characteristicUuidString];

  return (int)[[UnityBridgeFacade shared]
      subscribeToCharacteristicWithUUID:characteristicUuid
                             forService:serviceUuid
                           ofPeripheral:peripheralUuid];
}

int UnityBLE_UnsubscribeFromCharacteristic(const char *peripheralUUID,
                                           const char *serviceUUID,
                                           const char *characteristicUUID) {
  NSString *peripheralUuidString =
      [NSString stringWithUTF8String:peripheralUUID];
  NSString *serviceUuidString = [NSString stringWithUTF8String:serviceUUID];
  NSString *characteristicUuidString =
      [NSString stringWithUTF8String:characteristicUUID];
  NSUUID *peripheralUuid =
      [[NSUUID alloc] initWithUUIDString:peripheralUuidString];
  CBUUID *serviceUuid = [CBUUID UUIDWithString:serviceUuidString];
  CBUUID *characteristicUuid = [CBUUID UUIDWithString:characteristicUuidString];

  return (int)[[UnityBridgeFacade shared]
      unsubscribeFromCharacteristicWithUUID:characteristicUuid
                                 forService:serviceUuid
                               ofPeripheral:peripheralUuid];
}

void UnityBLE_registerOnPeripheralDiscovered(
    OnPeripheralDiscoveredCallback callback) {
  [[UnityBridgeFacade shared] registerOnPeripheralDiscovered:^(NSString *json) {
    if (callback) {
      callback([json UTF8String]);
    }
  }];
}

void UnityBLE_registerOnPeripheralConnected(
    OnPeripheralConnectedCallback callback) {
  [[UnityBridgeFacade shared] registerOnPeripheralConnected:^(NSString *uuid) {
    if (callback) {
      callback([uuid UTF8String]);
    }
  }];
}

void UnityBLE_registerOnPeripheralDisconnected(
    OnPeripheralDisconnectedCallback callback) {
  [[UnityBridgeFacade shared]
      registerOnPeripheralDisconnected:^(NSString *uuid) {
        if (callback) {
          callback([uuid UTF8String]);
        }
      }];
}

void UnityBLE_registerOnBleErrorDetected(OnBleErrorCallback callback) {
  [[UnityBridgeFacade shared]
      registerOnBleErrorDetected:^(NSString *errorJson) {
        if (callback) {
          callback([errorJson UTF8String]);
        }
      }];
}

void UnityBLE_registerOnBleStateChanged(OnBleStateChangedCallback callback) {
  [[UnityBridgeFacade shared] registerOnBleStateChanged:^(NSInteger state) {
    if (callback) {
      callback((int)state);
    }
  }];
}

void UnityBLE_registerOnDiscoveredPeripheralCleared(
    OnDiscoveredPeripheralClearedCallback callback) {
  [[UnityBridgeFacade shared] registerOnDiscoveredPeripheralCleared:^{
    if (callback) {
      callback();
    }
  }];
}

void UnityBLE_registerOnDiscoveredServices(
    OnDiscoveredServiceCallback callback) {
  [[UnityBridgeFacade shared] registerOnDiscoveredServices:^(NSString *json) {
    if (callback) {
      callback([json UTF8String]);
    }
  }];
}

void UnityBLE_registerOnDiscoveredCharacteristics(
    OnDiscoveredCharacteristicCallback callback) {
  [[UnityBridgeFacade shared]
      registerOnDiscoveredCharacteristics:^(NSString *json) {
        if (callback) {
          callback([json UTF8String]);
        }
      }];
}

void UnityBLE_registerOnLog(OnLogCallback callback) {
  [[UnityBridgeFacade shared] registerOnLog:^(NSString *logMessage) {
    if (callback) {
      callback([logMessage UTF8String]);
    }
  }];
}

void UnityBLE_registerOnWriteCharacteristicCompleted(
    OnWriteCharacteristicCompletedCallback callback) {
  [[UnityBridgeFacade shared]
      registerOnWriteCharacteristicCompleted:^(NSString *characteristicUUID,
                                               NSInteger result,
                                               NSString *errorDiscription) {
        if (callback) {
          callback([characteristicUUID UTF8String], (int)result,
                   [errorDiscription UTF8String]);
        }
      }];
}

void UnityBLE_registerOnReadRSSICompleted(
    OnReadRSSICompletedCallback callback) {
  [[UnityBridgeFacade shared]
      registerOnReadRSSICompleted:^(NSInteger result,
                                    NSString *errorDiscription) {
        if (callback) {
          callback((int)result, [errorDiscription UTF8String]);
        }
      }];
}

void UnityBLE_registerOnValueReceived(OnValueReceivedCallback callback) {
  [[UnityBridgeFacade shared]
      registerOnDataReceived:^(NSString *_Nonnull peripheralUuid,
                               NSString *_Nonnull valueHex) {
        if (callback) {
          callback([peripheralUuid UTF8String], [valueHex UTF8String]);
        }
      }];
}
}
