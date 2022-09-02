using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaMe.Core
{
    public class AppActivityFact
    {
        public string AppName { get; set; }
        public DateTime DateTime { get; set; } //date and hour in UTC.
        public double TotalDuration { get; set; }
        public double TotalIdleDuration { get; set; }
        public double TotalActiveDuration { get; set; }
        public int TotalItems { get; set; } //number of raw app activity items summarised within
    }
}
