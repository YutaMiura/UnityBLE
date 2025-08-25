package jp.yuta.miura.unityble.dto

import kotlinx.serialization.Serializable

@Serializable
data class ReadResponse (
    val from: String,
    val value: String,
    val status: Int
)

@Serializable
data class WriteResponse (
    val from: String,
    val status: Int
)

@Serializable
data class SubscribeResponse (
    val from: String,
    val value: String,
    val status: Int
)