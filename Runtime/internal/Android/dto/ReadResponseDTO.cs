using System;

namespace UnityBLE.Android
{
    [Serializable]
    public class ReadResponseDTO
    {
        public string from;
        public string value;
        public int status;
    }
}