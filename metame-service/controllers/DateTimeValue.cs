using System;
using System.Collections.Immutable;
using System.Linq;

namespace MetaMe.WindowsClient.controllers
{
    public class DateTimeValue
    {
        public DateTime DateTime { get; set; }
        public double Value { get; set; }

        public override string ToString()
        {
            return string.Format("DateTimeValue: {0} - {1}", DateTime, Value);
        }
        //stepSize is in hours
        public static ImmutableArray<DateTimeValue> Range(DateTime start, int count, int stepSizeMinutes)
        {
            return Enumerable.Range(0, count).Select(i => new DateTimeValue { DateTime = start.AddMinutes(i * stepSizeMinutes), Value = 0 }).ToImmutableArray();
        }

        public static ImmutableArray<DateTimeValue> RangeFromEnd(DateTime end, int count, int stepSizeMinutes)
        {
            var start = end.AddMinutes(-1 * (count - 1) * stepSizeMinutes);
            return Range(start, count, stepSizeMinutes);
        }
    }
}
