using System;

namespace MetaMe.Core
{
    public class ProcessActivityInfo : IDateRange
    {
        public string ProcessLocation { get; set; }
        public string ProcessName { get; set; }
        public string WindowText { get; set; }
        public string Url { get; set; }

        [Obsolete]
        public string AppName { get; set; }
        public DateTime Start { get; set; }
        public DateTime Stop { get; set; }

        public double GetDuration()
        {
            return Stop.Subtract(Start).TotalMilliseconds;
        }

    }
}
