using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaMe.WindowsClient.controllers
{
    public class AnalyticsProductivityOverview
    {
        public string DatesFooter { get; set; }
        public ImmutableArray<DateTimeValue> TotalProductiveTimeSeries { get; set; }
        public ImmutableArray<DateTimeValue> TotalActiveTimeSeries { get; set; }
    }
}
