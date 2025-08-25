package jp.yuta.miura.unityble

import android.Manifest
import android.app.Activity
import android.content.Context
import android.content.Intent
import android.content.pm.PackageManager
import android.location.LocationManager
import android.os.Build
import androidx.core.app.ActivityCompat
import jp.yuta.miura.unityble.unity.UnityLogger

object PermissionResultDispatcher {
    @Volatile
    private var listener: ((Boolean) -> Unit)? = null

    fun set(listener: (Boolean) -> Unit) {
        PermissionResultDispatcher.listener = listener
    }

    private fun clear() { listener = null }

    fun dispatch(granted: Boolean) {
        listener?.invoke(granted)
        clear()
    }
}


class PermissionService (private val activity: Activity){
    enum class PermissionResult {
        ReadyForUse, LocationServiceDisabled, SomePermissionsDined,
    }
    /**
     * Returns whether the required permissions for BLE usage are granted.
     * @return
     * - 0: All permissions are granted.
     * - -1: LocationService is not enabled (Only applicable for API level < 30)
     * - -2: One or more required permissions are not granted.
     */
    fun ensurePermissionsWithResult(permissions: Array<String>, callback: (PermissionResult)->Unit){
        UnityLogger.d("Call ensurePermissionsWithResult with ${permissions.joinToString()}")
        if(Build.VERSION.SDK_INT <= Build.VERSION_CODES.R) {
            if(!isLocationEnabled()) {
                callback(PermissionResult.LocationServiceDisabled)
                return
            }
        }

        if(permissions.all { isGranted(it) }) {
            callback(PermissionResult.ReadyForUse)
            return
        } else {
            val deniedPermissions = permissions.filter { !isGranted(it) }.toTypedArray()
            PermissionResultDispatcher.set { granted ->
                if (!granted) {
                    callback(PermissionResult.SomePermissionsDined)
                } else {
                    callback(PermissionResult.ReadyForUse)
                }
            }

            val intent = Intent(activity, PermissionRequester::class.java)
                .putExtra(PermissionRequester.EXTRA_PERMISSION, deniedPermissions)
            activity.startActivity(intent)
        }
    }

    fun requiredPermissionsForScan(): Array<String> {
        return when {
            Build.VERSION.SDK_INT >= Build.VERSION_CODES.S -> arrayOf(
                Manifest.permission.BLUETOOTH_SCAN
            )
            Build.VERSION.SDK_INT >= Build.VERSION_CODES.Q -> arrayOf(
                Manifest.permission.ACCESS_FINE_LOCATION
            )
            else -> arrayOf (
                Manifest.permission.ACCESS_COARSE_LOCATION
            )
        }
    }

    fun requiredPermissionsForConnect() : Array<String> {
        return when {
            Build.VERSION.SDK_INT >= Build.VERSION_CODES.S -> arrayOf(
                Manifest.permission.BLUETOOTH_CONNECT
            )
            else -> emptyArray()
        }
    }

    private fun isGranted(permission: String): Boolean {
        return ActivityCompat.checkSelfPermission(
            activity,
            permission
        ) == PackageManager.PERMISSION_GRANTED
    }

    private fun isLocationEnabled(): Boolean {
        val lm = activity.applicationContext.getSystemService(Context.LOCATION_SERVICE) as LocationManager
        return if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.P) {
            kotlin.runCatching {
                lm.isLocationEnabled
            }.getOrElse { false }
        } else {
            kotlin.runCatching {
                lm.isProviderEnabled(LocationManager.GPS_PROVIDER) || lm.isProviderEnabled(LocationManager.NETWORK_PROVIDER)
            }.getOrElse { false }
        }
    }
}