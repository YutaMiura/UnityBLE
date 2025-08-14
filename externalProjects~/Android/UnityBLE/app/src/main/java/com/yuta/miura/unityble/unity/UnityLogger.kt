package com.yuta.miura.unityble.unity

class UnityLogger {
    companion object {
        const val GAME_OBJ_NAME = "UnityBLELogReceiver"
        const val METHOD_NAME_FOR_DEBUG = "OnReceiveLogDebug"
        const val METHOD_NAME_FOR_WARN = "OnReceiveLogWarn"
        const val METHOD_NAME_FOR_ERR = "OnReceiveLogError"
        fun d(msg: String) {
            UnityFacade.sendToUnity(GAME_OBJ_NAME, METHOD_NAME_FOR_DEBUG, msg)
        }

        fun w(msg: String) {
            UnityFacade.sendToUnity(GAME_OBJ_NAME, METHOD_NAME_FOR_WARN, msg)
        }

        fun e(msg: String) {
            UnityFacade.sendToUnity(GAME_OBJ_NAME, METHOD_NAME_FOR_ERR, msg)
        }
    }
}