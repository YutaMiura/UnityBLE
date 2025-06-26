package unityble;

import android.bluetooth.BluetoothAdapter;
import android.bluetooth.BluetoothManager;
import android.bluetooth.le.BluetoothLeScanner;
import android.bluetooth.le.ScanCallback;
import android.bluetooth.le.ScanFilter;
import android.bluetooth.le.ScanResult;
import android.bluetooth.le.ScanSettings;
import android.content.Context;
import android.os.ParcelUuid;
import android.util.Log;

import java.util.ArrayList;
import java.util.List;

public class BlePlugin {
    private static final String TAG = "BlePlugin";
    private static BlePlugin instance;

    private BluetoothAdapter bluetoothAdapter;
    private BluetoothLeScanner bluetoothLeScanner;
    private boolean isScanning = false;
    private ScanCallback scanCallback;

    private BlePlugin() {
        // Private constructor for singleton
    }

    public static BlePlugin getInstance() {
        if (instance == null) {
            instance = new BlePlugin();
        }
        return instance;
    }

    public boolean initialize(Context context) {
        if (context == null) {
            Log.e(TAG, "Context is null");
            return false;
        }

        BluetoothManager bluetoothManager = (BluetoothManager) context.getSystemService(Context.BLUETOOTH_SERVICE);
        if (bluetoothManager == null) {
            Log.e(TAG, "BluetoothManager is not available");
            return false;
        }

        bluetoothAdapter = bluetoothManager.getAdapter();
        if (bluetoothAdapter == null) {
            Log.e(TAG, "Bluetooth adapter is not available");
            return false;
        }

        bluetoothLeScanner = bluetoothAdapter.getBluetoothLeScanner();
        if (bluetoothLeScanner == null) {
            Log.e(TAG, "BluetoothLeScanner is not available");
            return false;
        }

        // Initialize scan callback
        scanCallback = new ScanCallback() {
            @Override
            public void onScanResult(int callbackType, ScanResult result) {
                super.onScanResult(callbackType, result);
                Log.d(TAG, "Device found: " + result.getDevice().getName() + " (" + result.getDevice().getAddress() + ")");
                // Here you would typically notify Unity about the found device
            }

            @Override
            public void onScanFailed(int errorCode) {
                super.onScanFailed(errorCode);
                Log.e(TAG, "Scan failed with error code: " + errorCode);
                isScanning = false;
            }
        };

        Log.d(TAG, "BlePlugin initialized successfully");
        return true;
    }

    public boolean startScan(long duration, String serviceUuids, String nameFilter) {
        if (bluetoothAdapter == null || !bluetoothAdapter.isEnabled()) {
            Log.e(TAG, "Bluetooth adapter is null or disabled");
            return false;
        }

        if (isScanning) {
            Log.w(TAG, "Scan already in progress");
            return false;
        }

        // Parse service UUIDs if provided
        List<ParcelUuid> serviceUuidList = null;
        if (serviceUuids != null && !serviceUuids.isEmpty()) {
            serviceUuidList = new ArrayList<>();
            String[] uuids = serviceUuids.split(",");
            for (String uuid : uuids) {
                try {
                    serviceUuidList.add(ParcelUuid.fromString(uuid.trim()));
                } catch (IllegalArgumentException e) {
                    Log.w(TAG, "Invalid UUID format: " + uuid);
                }
            }
        }

        // Create scan filter if service UUIDs or name filter is provided
        ScanFilter.Builder filterBuilder = new ScanFilter.Builder();
        if (serviceUuidList != null && !serviceUuidList.isEmpty()) {
            filterBuilder.setServiceUuid(serviceUuidList.get(0));
        }
        if (nameFilter != null && !nameFilter.isEmpty()) {
            filterBuilder.setDeviceName(nameFilter);
        }
        ScanFilter scanFilter = filterBuilder.build();

        // Create scan settings
        ScanSettings.Builder settingsBuilder = new ScanSettings.Builder()
                .setScanMode(ScanSettings.SCAN_MODE_LOW_LATENCY);

        // Start scanning
        List<ScanFilter> filters = new ArrayList<>();
        if (serviceUuidList != null && !serviceUuidList.isEmpty() ||
            (nameFilter != null && !nameFilter.isEmpty())) {
            filters.add(scanFilter);
        }

        try {
            bluetoothLeScanner.startScan(filters, settingsBuilder.build(), scanCallback);
            isScanning = true;
            Log.d(TAG, "BLE scan started");
            return true;
        } catch (Exception e) {
            Log.e(TAG, "Failed to start scan: " + e.getMessage());
            return false;
        }
    }

    public void stopScan() {
        if (bluetoothLeScanner != null && isScanning) {
            try {
                bluetoothLeScanner.stopScan(scanCallback);
                isScanning = false;
                Log.d(TAG, "BLE scan stopped");
            } catch (Exception e) {
                Log.e(TAG, "Failed to stop scan: " + e.getMessage());
            }
        }
    }

    public boolean isScanning() {
        return isScanning;
    }
}