package com.yuta.miura.unityble.unity

import com.unity3d.player.UnityPlayer

class UnityFacade {
    companion object {
        fun sendToUnity(gameObj: String, methodName: String, msg: String) {
            UnityPlayer.UnitySendMessage(gameObj, methodName, msg)
        }
    }
}