using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaMe.WindowsClient
{
    public class GoalPerformanceDay
    {
        public DateTime DateTime { get; set; }
        public double Value { get; set; }
        public bool HasGoal { get; set; }
        public bool IsGoalMet { get; set; }
    }
}
