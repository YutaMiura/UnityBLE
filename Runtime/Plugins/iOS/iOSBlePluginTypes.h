#ifndef iOSBlePluginTypes_h
#define iOSBlePluginTypes_h

// Unity callback delegate types
typedef void (*UnityDeviceDiscoveredCallback)(const char* deviceJson);
typedef void (*UnityDeviceConnectedCallback)(const char* deviceJson);
typedef void (*UnityDeviceDisconnectedCallback)(const char* deviceJson);
typedef void (*UnityScanCompletedCallback)();
typedef void (*UnityCharacteristicValueCallback)(const char* characteristicJson, const char* valueHex);
typedef void (*UnityErrorCallback)(const char* errorMessage);
typedef void (*UnityLogCallback)(const char* logMessage);
typedef void (*UnityServicesDiscoveredCallback)(const char* deviceAddress);

#endif /* iOSBlePluginTypes_h */