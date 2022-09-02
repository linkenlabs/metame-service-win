using System;

namespace MetaMe.Sensors
{
    class IdleEvent
    {
        public string Type { get; set; }
        public IdleStateEnum State { get; set; }
        public DateTime Timestamp { get; set; } // UTC
    }
}
