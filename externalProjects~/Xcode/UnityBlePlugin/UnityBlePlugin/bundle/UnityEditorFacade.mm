#import <Foundation/Foundation.h>
#import <UnityBlePlugin/UnityBridge.h>

int UnityBLEBundle_StartScanning(const char **serviceUUIDs,
                                 const char *nameFilter) {
  return UnityBLE_StartScanning(serviceUUIDs, nameFilter);
}

void UnityBLEBundle_StopScanning() { UnityBLE_StopScanning(); }

int UnityBLEBundle_DisconnectFromPeripheral(const char *peripheralUUID) {
  return UnityBLE_DisconnectFromPeripheral(peripheralUUID);
}

bool UnityBLEBundle_IsScanning() { return UnityBLE_IsScanning(); }

int UnityBLEBundle_ReadCharacteristic(const char *peripheralUUID,
                                      const char *serviceUUID,
                                      const char *characteristicUUID) {
  return UnityBLE_ReadCharacteristic(peripheralUUID, serviceUUID,
                                     characteristicUUID);
}

int UnityBLEBundle_WriteCharacteristic(const char *peripheralUUID,
                                       const char *serviceUUID,
                                       const char *characteristicUUID,
                                       const unsigned char *data,
                                       int length) {
  return UnityBLE_WriteCharacteristic(peripheralUUID, serviceUUID,
                                      characteristicUUID, data, length);
}

int UnityBLEBundle_SubscribeToCharacteristic(const char *peripheralUUID,
                                             const char *serviceUUID,
                                             const char *characteristicUUID) {
  return UnityBLE_SubscribeToCharacteristic(peripheralUUID, serviceUUID,
                                            characteristicUUID);
}

int UnityBLEBundle_UnsubscribeFromCharacteristic(
    const char *peripheralUUID, const char *serviceUUID,
    const char *characteristicUUID) {
  return UnityBLE_UnsubscribeFromCharacteristic(peripheralUUID, serviceUUID,
                                                characteristicUUID);
}

void UnityBLEBundle_registerOnPeripheralDiscovered(
    OnPeripheralDiscoveredCallback callback) {
  UnityBLE_registerOnPeripheralDiscovered(callback);
}

void UnityBLEBundle_registerOnPeripheralConnected(
    OnPeripheralConnectedCallback callback) {
  UnityBLE_registerOnPeripheralConnected(callback);
}

void UnityBLEBundle_registerOnPeripheralDisconnected(
    OnPeripheralDisconnectedCallback callback) {
  UnityBLE_registerOnPeripheralDisconnected(callback);
}

void UnityBLEBundle_registerOnBleStateChanged(
    OnBleStateChangedCallback callback) {
  UnityBLE_registerOnBleStateChanged(callback);
}

void UnityBLEBundle_registerOnDiscoveredPeripheralCleared(
    OnDiscoveredPeripheralClearedCallback callback) {
  UnityBLE_registerOnDiscoveredPeripheralCleared(callback);
}

void UnityBLEBundle_registerOnDiscoveredServices(
    OnDiscoveredServiceCallback callback) {
  UnityBLE_registerOnDiscoveredServices(callback);
}

void UnityBLEBundle_registerOnDiscoveredCharacteristics(
    OnDiscoveredCharacteristicCallback callback) {
  UnityBLE_registerOnDiscoveredCharacteristics(callback);
}

void UnityBLEBundle_registerOnLog(OnLogCallback callback) {
  UnityBLE_registerOnLog(callback);
}

void UnityBLEBundle_registerOnWriteCharacteristicCompleted(
    OnWriteCharacteristicCompletedCallback callback) {
  UnityBLE_registerOnWriteCharacteristicCompleted(callback);
}

void UnityBLEBundle_registerOnReadRSSICompleted(
    OnReadRSSICompletedCallback callback) {
  UnityBLE_registerOnReadRSSICompleted(callback);
}

void UnityBLEBundle_registerOnValueReceived(OnValueReceivedCallback callback) {
  UnityBLE_registerOnValueReceived(callback);
}
