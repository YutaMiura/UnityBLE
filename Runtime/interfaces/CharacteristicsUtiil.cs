namespace UnityBLE
{
    public static class CharacteristicsUtil
    {
        public static string GetCharacteristicName(string uuid)
        {
            return uuid.ToLower() switch
            {
                "00002a00-0000-1000-8000-00805f9b34fb" => "Device Name",
                "00002a01-0000-1000-8000-00805f9b34fb" => "Appearance",
                "00002a02-0000-1000-8000-00805f9b34fb" => "Peripheral Privacy Flag",
                "00002a05-0000-1000-8000-00805f9b34fb" => "Service Changed",
                "00002a19-0000-1000-8000-00805f9b34fb" => "Battery Level",
                "00002a24-0000-1000-8000-00805f9b34fb" => "Model Number",
                "00002a25-0000-1000-8000-00805f9b34fb" => "Serial Number",
                "00002a26-0000-1000-8000-00805f9b34fb" => "Firmware Revision",
                "00002a27-0000-1000-8000-00805f9b34fb" => "Hardware Revision",
                "00002a28-0000-1000-8000-00805f9b34fb" => "Software Revision",
                "00002a29-0000-1000-8000-00805f9b34fb" => "Manufacturer Name",
                _ => $"Characteristic {uuid[..8]}"
            };
        }
    }
}