using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaMe.Sensors
{
    class AppActivityEvent
    {
        public string AppName { get; set; }
        public DateTime Timestamp { get; set; } //should be UTC time
    }
}
