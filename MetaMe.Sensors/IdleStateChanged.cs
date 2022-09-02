using MetaMe.Core;
using System;

namespace MetaMe.Sensors
{
    class IdleStateChanged
    {
        public DateTime Timestamp { get; set; }
        public IdleStateEnum State { get; set; }
        public IdleActivityInfo IdleActivityInfo { get; set; }
    }
}
