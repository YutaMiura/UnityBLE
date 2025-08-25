using System;

namespace UnityBLE.Android
{
    [Serializable]
    public class SubscribeResponseDTO
    {
        public string from;
        public string value;
        public int status;
    }
}