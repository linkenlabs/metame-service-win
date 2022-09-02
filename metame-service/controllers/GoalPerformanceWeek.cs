using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaMe.WindowsClient.controllers
{
    public class GoalPerformanceWeek
    {
        public DateTime WeekStart { get; set; }
        public ImmutableArray<GoalPerformanceDay> DayPerformance { get; set; }
    }
}
