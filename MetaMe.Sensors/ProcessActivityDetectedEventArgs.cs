using MetaMe.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaMe.Sensors
{
    class ProcessActivityDetectedEventArgs: EventArgs
    {
        public ProcessActivityInfo Previous { get; set; }
        public ProcessActivityInfo Current { get; set; }
    }
}
