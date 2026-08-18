package jp.yuta.miura.unityble

import android.app.Activity
import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.result.contract.ActivityResultContracts
import androidx.core.app.ActivityCompat
import jp.yuta.miura.unityble.unity.UnityLogger

class PermissionRequester : ComponentActivity() {
    private val requestLauncher = registerForActivityResult(
        ActivityResultContracts.RequestMultiplePermissions()
    ) {results ->
        val deniedPermissions = results.filterValues { !it }.keys
        val result = when {
            deniedPermissions.isEmpty() -> PermissionRequestResult.Granted
            deniedPermissions.any { permission ->
                !ActivityCompat.shouldShowRequestPermissionRationale(this, permission)
            } -> PermissionRequestResult.PermanentlyDenied
            else -> PermissionRequestResult.Denied
        }
        PermissionResultDispatcher.dispatch(result)
        if(result == PermissionRequestResult.Granted) {
            setResult(Activity.RESULT_OK, intent)
        } else {
            setResult(Activity.RESULT_CANCELED, intent)
        }
        finish()
    }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        val permission = intent.getStringArrayExtra(EXTRA_PERMISSION)
        UnityLogger.d("PermissionRequester.onCreate $permission")
        if(permission.isNullOrEmpty()) {
            UnityLogger.d("PermissionRequester.onCreate -> Canceled")
            setResult(Activity.RESULT_CANCELED)
            finish()
        } else {
            UnityLogger.d("PermissionRequester.onCreate -> Request $permission")
            requestLauncher.launch(permission)
        }
    }

    companion object {
        const val EXTRA_PERMISSION = "jp.yuta.miura.unityble.PermissionRequester.permission"
    }
}
