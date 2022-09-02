using System;

namespace MetaMe.Core
{
    public class IdleActivityInfo: IDateRange
    {
        public DateTime Start { get; set; }
        public DateTime Stop { get; set; }
        public string Type { get; set; } //null = default. 

        public override string ToString()
        {
            var duration = Stop.Subtract(Start).TotalSeconds;

            return String.Format("{0:yyyy-MM-dd HH:mm:ss} - {1:0.00}s", Start, duration);
        }

        public double GetDuration()
        {
            return Stop.Subtract(Start).TotalMilliseconds;
        }
    }
}
