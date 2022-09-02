using MetaMe.Sensors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaMe.Sensors
{
    class ForegroundChangedEventArgs
    {
        public ProcessInfo ProcessInfo { get; set; }
        public DateTime DateTime { get; set; }
        public string ClassName { get; set; }
        public IntPtr WindowHandle { get; set; }

    }
}
