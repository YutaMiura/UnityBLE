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

        // Base64-encoded Manufacturer Specific Data from the advertisement.
        // On iOS: bytes from kCBAdvertisementDataManufacturerDataKey (includes
        // the 2-byte company ID prefix as advertised). On Android: each MSD
        // SparseArray entry encoded as [companyId LE 2 bytes][payload],
        // concatenated. Empty/null when the advertisement has no MSD.
        //
        // NOTE: the native JSON may also carry a "serviceData" object map.
        // JsonUtility silently drops it (no Dictionary<,> support); add a
        // parallel-array shim here if/when managed code needs to read it.
        [SerializeField]
        public string manufacturerData;

        public static PeripheralDTO FromJson(string json)
        {
            Debug.Log(json);
            return JsonUtility.FromJson<PeripheralDTO>(json);
        }
    }
}