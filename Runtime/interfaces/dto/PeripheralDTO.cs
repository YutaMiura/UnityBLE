using System;
using UnityEngine;

namespace UnityBLE
{
    [Serializable]
    internal class PeripheralDTO
    {
        [SerializeField]
        public string uuid;
        [SerializeField]
        public string name;
        [SerializeField]
        public int rssi;

        public static PeripheralDTO FromJson(string json)
        {
            Debug.Log(json);
            return JsonUtility.FromJson<PeripheralDTO>(json);
        }
    }
}