using UnityEngine;

namespace UnityBLE
{
    public static class AndroidVersion
    {
        public static int ApiLevel
        {
            get
            {
                using var version = new AndroidJavaClass("android.os.Build$VERSION");
                return version.GetStatic<int>("SDK_INT");
            }
        }
    }
}