using System;

namespace MetaMe.Core
{
    static class DateExtensions
    {

        public static DateTime ToPeriodStart(this DateTime input, int periodMins)
        {
            if (periodMins > 60 * 24)
            {
                throw new NotImplementedException();
            }
            var totalDayMinutes = input.Hour * 60 + input.Minute;
            var periodCount = totalDayMinutes / periodMins;
            var periodMinutes = periodCount * periodMins;

            var hour = periodMinutes / 60;
            var minutes = periodMinutes - (hour * 60);

            return new DateTime(input.Year, input.Month, input.Day, hour, minutes, 0, input.Kind);
        }

        public static DateTime ToPeriodEnd(this DateTime input, int periodMins)
        {
            var periodStart = input.ToPeriodStart(periodMins);
            var end = periodStart.AddMinutes(periodMins);
            return end;
        }

        //pre: input is UTC dateKind already
        public static DateTime ToDayStart(this DateTime input, TimeSpan utcStartTime)
        {
            if (input.TimeOfDay < utcStartTime)
            {
                //its the previous day
                return input.Date.Add(utcStartTime).AddDays(-1);
            }
            else
            {
                return input.Date.Add(utcStartTime);
            }
        }

        public static DateTime ToWeekStart(this DateTime utcDate, DayOfWeek startOfWeek, TimeSpan utcStartTime)
        {
            int diff = (7 + (utcDate.DayOfWeek - startOfWeek)) % 7;

            var weekStart = utcDate.AddDays(-1 * diff);

            return weekStart.ToDayStart(utcStartTime);
        }
    }
}
