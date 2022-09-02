using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaMe.WindowsClient
{
    public class TimeVector
    {
        public DateTime PeriodStart { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsActive { get; set; }
        public double Value { get; set; }
    }
}
