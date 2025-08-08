#ifndef iOSBlePluginBridge_h
#define iOSBlePluginBridge_h

#import <Foundation/Foundation.h>
#import "iOSBlePluginTypes.h"

#ifdef __cplusplus
extern "C" {
#endif

// Callback setters
void iOSBlePlugin_SetDeviceDiscoveredCallback(UnityDeviceDiscoveredCallback callback);
void iOSBlePlugin_SetDeviceConnectedCallback(UnityDeviceConnectedCallback callback);
void iOSBlePlugin_SetDeviceDisconnectedCallback(UnityDeviceDisconnectedCallback callback);
void iOSBlePlugin_SetScanCompletedCallback(UnityScanCompletedCallback callback);
void iOSBlePlugin_SetCharacteristicValueCallback(UnityCharacteristicValueCallback callback);
void iOSBlePlugin_SetErrorCallback(UnityErrorCallback callback);
void iOSBlePlugin_SetLogCallback(UnityLogCallback callback);
void iOSBlePlugin_SetServicesDiscoveredCallback(UnityServicesDiscoveredCallback callback);

// Plugin operations
BOOL iOSBlePlugin_Initialize();
BOOL iOSBlePlugin_IsBluetoothReady();
BOOL iOSBlePlugin_WaitForBluetoothReady(double timeout);
BOOL iOSBlePlugin_StartScan(double duration);
void iOSBlePlugin_StopScan();
BOOL iOSBlePlugin_ConnectToDevice(const char* address);
BOOL iOSBlePlugin_DisconnectFromDevice(const char* address);

// Service and characteristic operations
const char** iOSBlePlugin_GetServices(const char* address, int* count);
const char** iOSBlePlugin_GetCharacteristics(const char* address, const char* serviceUUID, int* count);
BOOL iOSBlePlugin_ReadCharacteristic(const char* address, const char* serviceUUID, const char* characteristicUUID);
BOOL iOSBlePlugin_WriteCharacteristic(const char* address, const char* serviceUUID, const char* characteristicUUID, const char* dataHex);
BOOL iOSBlePlugin_WriteCharacteristicWithResponse(const char* address, const char* serviceUUID, const char* characteristicUUID, const char* dataHex, BOOL withResponse);
BOOL iOSBlePlugin_SubscribeCharacteristic(const char* address, const char* serviceUUID, const char* characteristicUUID);
BOOL iOSBlePlugin_UnsubscribeCharacteristic(const char* address, const char* serviceUUID, const char* characteristicUUID);
const char* iOSBlePlugin_GetCharacteristicProperties(const char* address, const char* serviceUUID, const char* characteristicUUID);

#ifdef __cplusplus
}
#endif

#endif /* iOSBlePluginBridge_h */