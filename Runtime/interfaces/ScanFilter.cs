using System;

namespace UnityBLE
{
    public class ScanFilter
    {
        public string[] ServiceUuids { get; set; }
        public string Name { get; set; }

        public ScanFilter(string[] serviceUuids = null, string name = null)
        {
            ServiceUuids = serviceUuids ?? Array.Empty<string>();
            Name = name ?? string.Empty;
        }

        public bool NoFilter()
        {
            return ServiceUuids.Length == 0 && string.IsNullOrEmpty(Name);
        }
    }
}