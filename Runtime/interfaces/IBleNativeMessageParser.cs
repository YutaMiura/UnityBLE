using System;
using System.Collections.Generic;

namespace UnityBLE
{
    public interface IBleNativeMessageParser
    {
        IBleDevice ParseDeviceData(string deviceJson);
        IReadOnlyList<IBleService> ParseServicesData(string servicesJson);
        Exception ParseErrorMessage(string errorJson);
    }
}