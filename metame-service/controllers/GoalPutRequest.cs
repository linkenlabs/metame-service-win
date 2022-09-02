using MetaMe.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaMe.WindowsClient.controllers
{
    public class GoalPutRequest
    {
        public string Name { get; set; }
        public string Reason { get; set; }
        public GoalSubject Subject { get; set; }
        public string GoalType { get; set; }
        public int GoalValue { get; set; } //mins
        public string[] RepeatDaysOfWeek { get; set; }
    }
}
