using System;

namespace MetaMe.WindowsClient
{
    public class GoalProgressNotification
    {
        public string GoalKey { get; set; }
        public int GoalValue { get; set; } // mins, primary key

        public double ProgressTarget { get; set; } //Percentage between 0-1
        public double Progress { get; set; } //Percentage between 0-1
        public DateTime Created { get; set; }

    }
}
