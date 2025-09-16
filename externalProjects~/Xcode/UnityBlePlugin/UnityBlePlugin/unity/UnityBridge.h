#ifndef UnityBridge_h
#define UnityBridge_h

#ifdef __cplusplus
extern "C" {
#endif

typedef void (*OnPeripheralDiscoveredCallback)(const char *peripheralDtoJson);
typedef void (*OnPeripheralConnectedCallback)(const char *peripheralUuid);
typedef void (*OnPeripheralDisconnectedCallback)(const char *peripheralUuid);
typedef void (*OnBleErrorCallback)(const char *errorJson);
typedef void (*OnBleStateChangedCallback)(int state);
typedef void (*OnDiscoveredPeripheralClearedCallback)();
typedef void (*OnDiscoveredServiceCallback)(const char *serviceDtoJson);
typedef void (*OnDiscoveredCharacteristicCallback)(
    const char *characteristicDtoJson);
typedef void (*OnLogCallback)(const char *logMessage);
typedef void (*OnWriteCharacteristicCompletedCallback)(
    const char *characteristicUUID, int result, const char *errorDescription);
typedef void (*OnReadRSSICompletedCallback)(int result,
                                            const char *errorDescription);
typedef void (*OnValueReceivedCallback)(const char *peripheralUuid,
                                        const char *valueHex);

int UnityBLE_StartScanning(const char **serviceUUIDs, const char *nameFilter);
void UnityBLE_StopScanning();
int UnityBLE_ConnectToPeripheral(const char *peripheralUUID);
int UnityBLE_DisconnectFromPeripheral(const char *peripheralUUID);
int UnityBLE_ReadCharacteristic(const char *peripheralUUID,
                                const char *serviceUUID,
                                const char *characteristicUUID);
int UnityBLE_WriteCharacteristic(const char *peripheralUUID,
                                 const char *serviceUUID,
                                 const char *characteristicUUID,
                                 const unsigned char *data, int length);
int UnityBLE_SubscribeToCharacteristic(const char *peripheralUUID,
                                       const char *serviceUUID,
                                       const char *characteristicUUID);
int UnityBLE_UnsubscribeFromCharacteristic(const char *peripheralUUID,
                                           const char *serviceUUID,
                                           const char *characteristicUUID);
bool UnityBLE_IsScanning();

void UnityBLE_registerOnPeripheralDiscovered(
    OnPeripheralDiscoveredCallback callback);
void UnityBLE_registerOnPeripheralConnected(
    OnPeripheralConnectedCallback callback);
void UnityBLE_registerOnPeripheralDisconnected(
    OnPeripheralDisconnectedCallback callback);
void UnityBLE_registerOnBleErrorDetected(OnBleErrorCallback callback);
void UnityBLE_registerOnBleStateChanged(OnBleStateChangedCallback callback);
void UnityBLE_registerOnDiscoveredPeripheralCleared(
    OnDiscoveredPeripheralClearedCallback callback);
void UnityBLE_registerOnDiscoveredServices(
    OnDiscoveredServiceCallback callback);
void UnityBLE_registerOnDiscoveredCharacteristics(
    OnDiscoveredCharacteristicCallback callback);
void UnityBLE_registerOnLog(OnLogCallback callback);
void UnityBLE_registerOnWriteCharacteristicCompleted(
    OnWriteCharacteristicCompletedCallback callback);
void UnityBLE_registerOnReadRSSICompleted(OnReadRSSICompletedCallback callback);
void UnityBLE_registerOnValueReceived(OnValueReceivedCallback callback);
int UnityBLE_discoverServices(const char *peripheralUUID);

#ifdef __cplusplus
}
#endif

#endif /* UnityBridge_h */
