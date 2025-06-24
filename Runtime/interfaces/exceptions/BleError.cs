using System;

namespace UnityBLE
{
    public class BleError : Exception
    {
        public readonly string ErrorCode;
        public readonly string Details;

        public BleError(string message, string errorCode, string details) : base(message)
        {
            ErrorCode = errorCode;
            Details = details;
        }

        public BleError(string message, string errorCode, string details, Exception innerException)
            : base(message, innerException)
        {
            ErrorCode = errorCode;
            Details = details;
        }

    }
}