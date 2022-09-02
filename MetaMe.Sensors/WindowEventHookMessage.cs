using System;

namespace MetaMe.Sensors
{
    public class WindowEventHookMessage
    {
        public WindowEventHookMessageType Type { get; set; }
        public IntPtr WindowHandle { get; set; } //hwnd
        public uint EventTime { get; set; } //dwmsEventTime
        public int IdObject { get; set; } //idObject
        public uint EventType { get; set; } //eventType
        public int IdChild { get; set; }
    }
}
