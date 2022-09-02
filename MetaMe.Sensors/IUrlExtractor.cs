using System;

namespace MetaMe.Sensors
{
    interface IUrlExtractor
    {
        bool CanExtract(IntPtr windowHandle, string processName, string className, string windowText);
        string ExtractUrl(IntPtr windowHandle);
    }
}
