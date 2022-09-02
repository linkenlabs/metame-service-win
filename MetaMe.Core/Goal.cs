using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaMe.Core
{
    public class Goal
    {
        public string Key { get; set; }

        public string Name { get; set; }
        public string Reason { get; set; }
        public GoalSubject Subject { get; set; }
        public string GoalType { get; set; }
        public int GoalValue { get; set; } //mins
        public string[] RepeatDaysOfWeek { get; set; }
        public DateTime Created { get; set; }
    }
}
