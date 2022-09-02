using System;

namespace MetaMe.Sensors
{
    class ForegroundSensorMessage
    {
        public ForegroundSensorMessageType Type { get; set; }
        public IntPtr WindowHandle { get; set; } //hwnd
        public uint EventTime { get; set; } //dwmsEventTime
        public int IdObject { get; set; } //idObject
        public string ClassName { get; set; }
        public uint EventType { get; set; } //eventType
        public int IdChild { get; set; }
        public string ProcessName { get; set; }
        public uint ProcessId { get; set; }
        public string ProcessPath { get; set; }
    }
}
