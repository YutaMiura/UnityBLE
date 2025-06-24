package unityble;

import android.bluetooth.BluetoothAdapter;
import android.bluetooth.BluetoothDevice;
import android.bluetooth.BluetoothGatt;
import android.bluetooth.BluetoothGattCallback;
import android.bluetooth.BluetoothGattService;
import android.bluetooth.BluetoothManager;
import android.bluetooth.BluetoothProfile;
import android.bluetooth.le.BluetoothLeScanner;
import android.bluetooth.le.ScanCallback;
import android.bluetooth.le.ScanFilter;
import android.bluetooth.le.ScanResult;
import android.bluetooth.le.ScanSettings;
import android.content.Context;
import android.os.Build;
import android.os.ParcelUuid;
import android.util.Base64;
import android.util.Log;
import com.unity3d.player.UnityPlayer;
import org.json.JSONArray;
import org.json.JSONException;
import org.json.JSONObject;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.HashSet;
import java.util.List;
import java.util.Map;
import java.util.Set;
import java.util.Timer;
import java.util.TimerTask;
import java.util.UUID;

public class BlePlugin {
    private static final String TAG = "BlePlugin";
    private static final String UNITY_GAME_OBJECT = "BleManager";
    private static final String ON_DEVICE_DISCOVERED = "OnDeviceDiscovered";
    private static final String ON_SCAN_COMPLETED = "OnScanCompleted";
    private static final String ON_SCAN_FAILED = "OnScanFailed";
    private static final String ON_DEVICE_CONNECTED = "OnDeviceConnected";
    private static final String ON_DEVICE_DISCONNECTED = "OnDeviceDisconnected";
    private static final String ON_CONNECTION_FAILED = "OnConnectionFailed";
    private static final String ON_SERVICES_DISCOVERED = "OnServicesDiscovered";

    private static BlePlugin instance;

    private BluetoothAdapter bluetoothAdapter;
    private BluetoothLeScanner bluetoothLeScanner;
    private boolean isScanning = false;
    private ScanCallback scanCallback;
    private Set<String> discoveredDevices = new HashSet<>();
    private List<ScanFilter> scanFilters;
    private ScanSettings scanSettings;

    // Connection management
    private Map<String, BluetoothGatt> connectedDevices = new HashMap<>();
    private Map<String, BluetoothGattCallback> gattCallbacks = new HashMap<>();

    private BlePlugin() {
        initializeBluetooth();
    }

    public static BlePlugin getInstance() {
        if (instance == null) {
            instance = new BlePlugin();
        }
        return instance;
    }

    private void initializeBluetooth() {
        BluetoothManager bluetoothManager = (BluetoothManager) UnityPlayer.currentActivity.getSystemService(Context.BLUETOOTH_SERVICE);
        bluetoothAdapter = bluetoothManager.getAdapter();

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.LOLLIPOP) {
            bluetoothLeScanner = bluetoothAdapter.getBluetoothLeScanner();
        }
    }

    public boolean startScan(long duration, String serviceUuids, String nameFilter) {
        if (bluetoothAdapter == null || !bluetoothAdapter.isEnabled()) {
            sendMessageToUnity(ON_SCAN_FAILED, "Bluetooth is not enabled");
            return false;
        }

        if (isScanning) {
            Log.w(TAG, "Scan already in progress");
            return false;
        }

        // Clear previous results
        discoveredDevices.clear();

        // Create scan filters if needed
        scanFilters = createScanFilters(serviceUuids, nameFilter);

        // Create scan settings
        scanSettings = new ScanSettings.Builder()
            .setScanMode(ScanSettings.SCAN_MODE_LOW_LATENCY)
            .build();

        // Create scan callback
        scanCallback = new ScanCallback() {
            @Override
            public void onScanResult(int callbackType, ScanResult result) {
                super.onScanResult(callbackType, result);
                handleScanResult(result);
            }

            @Override
            public void onScanFailed(int errorCode) {
                super.onScanFailed(errorCode);
                String errorMessage;
                switch (errorCode) {
                    case SCAN_FAILED_ALREADY_STARTED:
                        errorMessage = "Scan already started";
                        break;
                    case SCAN_FAILED_APPLICATION_REGISTRATION_FAILED:
                        errorMessage = "Application registration failed";
                        break;
                    case SCAN_FAILED_FEATURE_UNSUPPORTED:
                        errorMessage = "Feature unsupported";
                        break;
                    case SCAN_FAILED_INTERNAL_ERROR:
                        errorMessage = "Internal error";
                        break;
                    default:
                        errorMessage = "Unknown error: " + errorCode;
                        break;
                }
                sendMessageToUnity(ON_SCAN_FAILED, errorMessage);
                isScanning = false;
            }
        };

        // Start scanning
        try {
            bluetoothLeScanner.startScan(scanFilters, scanSettings, scanCallback);
            isScanning = true;

            // Schedule scan stop
            Timer timer = new Timer();
            timer.schedule(new TimerTask() {
                @Override
                public void run() {
                    stopScan();
                }
            }, duration);

            Log.d(TAG, "BLE scan started for " + duration + "ms");
            return true;
        } catch (Exception e) {
            Log.e(TAG, "Failed to start scan", e);
            sendMessageToUnity(ON_SCAN_FAILED, "Failed to start scan: " + e.getMessage());
            return false;
        }
    }

    public void stopScan() {
        if (!isScanning || scanCallback == null) {
            return;
        }

        try {
            bluetoothLeScanner.stopScan(scanCallback);
            isScanning = false;
            scanCallback = null;
            Log.d(TAG, "BLE scan stopped");

            // Send completion message
            sendMessageToUnity(ON_SCAN_COMPLETED, "Scan completed");
        } catch (Exception e) {
            Log.e(TAG, "Failed to stop scan", e);
        }
    }

    // Connection methods
    public boolean connectToDevice(String deviceAddress) {
        if (bluetoothAdapter == null) {
            Log.e(TAG, "Bluetooth adapter is null");
            return false;
        }

        if (connectedDevices.containsKey(deviceAddress)) {
            Log.w(TAG, "Device already connected: " + deviceAddress);
            return true;
        }

        try {
            BluetoothDevice device = bluetoothAdapter.getRemoteDevice(deviceAddress);
            if (device == null) {
                Log.e(TAG, "Device not found: " + deviceAddress);
                return false;
            }

            BluetoothGattCallback gattCallback = new BluetoothGattCallback() {
                @Override
                public void onConnectionStateChange(BluetoothGatt gatt, int status, int newState) {
                    super.onConnectionStateChange(gatt, status, newState);

                    if (status == BluetoothGatt.GATT_SUCCESS) {
                        if (newState == BluetoothProfile.STATE_CONNECTED) {
                            Log.d(TAG, "Connected to device: " + gatt.getDevice().getAddress());
                            connectedDevices.put(gatt.getDevice().getAddress(), gatt);
                            sendMessageToUnity(ON_DEVICE_CONNECTED, gatt.getDevice().getAddress());

                            // Start service discovery
                            gatt.discoverServices();
                        } else if (newState == BluetoothProfile.STATE_DISCONNECTED) {
                            Log.d(TAG, "Disconnected from device: " + gatt.getDevice().getAddress());
                            connectedDevices.remove(gatt.getDevice().getAddress());
                            gattCallbacks.remove(gatt.getDevice().getAddress());
                            gatt.close();
                            sendMessageToUnity(ON_DEVICE_DISCONNECTED, gatt.getDevice().getAddress());
                        }
                    } else {
                        Log.e(TAG, "Connection failed with status: " + status);
                        sendMessageToUnity(ON_CONNECTION_FAILED, "Connection failed with status: " + status);
                        gatt.close();
                    }
                }

                @Override
                public void onServicesDiscovered(BluetoothGatt gatt, int status) {
                    super.onServicesDiscovered(gatt, status);

                    if (status == BluetoothGatt.GATT_SUCCESS) {
                        Log.d(TAG, "Services discovered for device: " + gatt.getDevice().getAddress());

                        // Create services JSON
                        JSONObject servicesJson = new JSONObject();
                        try {
                            servicesJson.put("deviceAddress", gatt.getDevice().getAddress());
                            JSONArray servicesArray = new JSONArray();

                            for (BluetoothGattService service : gatt.getServices()) {
                                JSONObject serviceJson = new JSONObject();
                                serviceJson.put("uuid", service.getUuid().toString());
                                serviceJson.put("type", service.getType());
                                servicesArray.put(serviceJson);
                            }

                            servicesJson.put("services", servicesArray);
                            sendMessageToUnity(ON_SERVICES_DISCOVERED, servicesJson.toString());
                        } catch (JSONException e) {
                            Log.e(TAG, "Error creating services JSON", e);
                        }
                    } else {
                        Log.e(TAG, "Service discovery failed with status: " + status);
                    }
                }
            };

            gattCallbacks.put(deviceAddress, gattCallback);
            BluetoothGatt gatt = device.connectGatt(UnityPlayer.currentActivity, false, gattCallback);

            if (gatt == null) {
                Log.e(TAG, "Failed to create GATT connection");
                gattCallbacks.remove(deviceAddress);
                return false;
            }

            Log.d(TAG, "Connecting to device: " + deviceAddress);
            return true;
        } catch (Exception e) {
            Log.e(TAG, "Error connecting to device: " + deviceAddress, e);
            return false;
        }
    }

    public boolean disconnectFromDevice(String deviceAddress) {
        BluetoothGatt gatt = connectedDevices.get(deviceAddress);
        if (gatt != null) {
            gatt.disconnect();
            return true;
        } else {
            Log.w(TAG, "Device not connected: " + deviceAddress);
            return false;
        }
    }

    public boolean isDeviceConnected(String deviceAddress) {
        return connectedDevices.containsKey(deviceAddress);
    }

    public String[] getConnectedDevices() {
        return connectedDevices.keySet().toArray(new String[0]);
    }

    private void handleScanResult(ScanResult result) {
        String deviceAddress = result.getDevice().getAddress();

        // Avoid duplicate devices
        if (discoveredDevices.contains(deviceAddress)) {
            return;
        }

        discoveredDevices.add(deviceAddress);

        // Create device info JSON
        JSONObject deviceInfo = new JSONObject();
        try {
            deviceInfo.put("address", deviceAddress);
            deviceInfo.put("name", result.getDevice().getName() != null ? result.getDevice().getName() : "Unknown");
            deviceInfo.put("rssi", result.getRssi());

            boolean isConnectable = true;
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
                isConnectable = result.isConnectable();
            }
            deviceInfo.put("isConnectable", isConnectable);

            // Add scan record data if available
            if (result.getScanRecord() != null) {
                deviceInfo.put("txPower", result.getScanRecord().getTxPowerLevel());
                byte[] bytes = result.getScanRecord().getBytes();
                if (bytes != null) {
                    deviceInfo.put("advertisingData", Base64.encodeToString(bytes, Base64.DEFAULT));
                }
            }
        } catch (JSONException e) {
            Log.e(TAG, "Error creating device JSON", e);
            return;
        }

        // Send to Unity
        sendMessageToUnity(ON_DEVICE_DISCOVERED, deviceInfo.toString());
    }

    private List<ScanFilter> createScanFilters(String serviceUuids, String nameFilter) {
        List<ScanFilter> filters = new ArrayList<>();

        // Add service UUID filter
        if (serviceUuids != null && !serviceUuids.isEmpty()) {
            String[] uuidList = serviceUuids.split(",");
            for (String uuid : uuidList) {
                try {
                    ScanFilter scanFilter = new ScanFilter.Builder()
                        .setServiceUuid(ParcelUuid.fromString(uuid.trim()))
                        .build();
                    filters.add(scanFilter);
                } catch (IllegalArgumentException e) {
                    Log.w(TAG, "Invalid UUID: " + uuid);
                }
            }
        }

        // Add name filter
        if (nameFilter != null && !nameFilter.isEmpty()) {
            ScanFilter scanFilter = new ScanFilter.Builder()
                .setDeviceName(nameFilter)
                .build();
            filters.add(scanFilter);
        }

        return filters;
    }

    private void sendMessageToUnity(String method, String data) {
        try {
            UnityPlayer.UnitySendMessage(UNITY_GAME_OBJECT, method, data);
        } catch (Exception e) {
            Log.e(TAG, "Failed to send message to Unity: " + method, e);
        }
    }

    public boolean isScanning() {
        return isScanning;
    }

    public int getDiscoveredDeviceCount() {
        return discoveredDevices.size();
    }
}