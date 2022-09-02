using MetaMe.Core;
using MetaMe.WindowsClient.controllers;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace MetaMe.WindowsClient
{
    static class AppActivityFactExtensions
    {
        const string TOTAL_SERIES_NAME = "Total";
        public static bool HasItemsCountInitialized(this IEnumerable<AppActivityFact> hourLevelFacts)
        {
            var uninitializedItem = (from item in hourLevelFacts
                                     where item.TotalItems == 0
                                     select item).FirstOrDefault();

            return uninitializedItem == null;
        }

        public static DateTime GetPointer(this ImmutableArray<AppActivityFact> items)
        {
            DateTime pointer = DateTime.MinValue;
            if (items.Length > 0)
            {
                var temp = (from f in items
                            select f.DateTime).Max();
                pointer = temp.AddMinutes(60);
            }
            return pointer;
        }

        public static ImmutableArray<AppActivityFact> FilterByGroup(this ImmutableArray<AppActivityFact> facts, string groupName)
        {
            if (groupName == TOTAL_SERIES_NAME)
            {
                return facts;
            }

            var matchingGroup = ClientApplication.Instance.GetGroups().First(d => d.Name == groupName);
            var relevantFacts = (from f in facts
                                 where matchingGroup.Items.Contains(f.AppName)
                                 select f).ToImmutableArray();
            return relevantFacts;
        }

        public static ImmutableArray<DateTimeValue> GetDateTimeValues(this ImmutableArray<AppActivityFact> appActivityFacts)
        {
            //group into DateTimeValues then merge;
            var groupRelevantValues = (from d in appActivityFacts
                                       group d by d.DateTime into g
                                       orderby g.Key ascending
                                       select new DateTimeValue
                                       {
                                           DateTime = g.Key,
                                           Value = g.Sum(d => d.TotalActiveDuration)
                                       }).ToImmutableArray();
            return groupRelevantValues;
        }

        //Pre: AppActivityFacts already stratified into appropriate intervals
        public static ImmutableArray<DateTimeValue> GetDateTimeValues2(this IEnumerable<AppActivityFact> appActivityFacts, DateTime start, int intervalMinutes, int length, string propName = "TotalActiveDuration")
        {
            var getPropValue = GetPropValueFunc(propName);
            var list = new List<DateTimeValue>();

            DateTime end = start.AddMinutes(intervalMinutes * length);

            for (var dateTime = start; dateTime < end; dateTime = dateTime.AddMinutes(intervalMinutes))
            {
                var matchingFacts = appActivityFacts.Where(fact =>
                {
                    return fact.DateTime == dateTime;
                });

                var duration = matchingFacts.Sum(getPropValue);
                list.Add(new DateTimeValue
                {
                    DateTime = dateTime,
                    Value = duration
                });
            }
            return list.ToImmutableArray();
        }


        public static ImmutableArray<DateTimeValue> GetDateTimeValuesByHour(this IEnumerable<AppActivityFact> hourLevelFacts, int limit, string propName = "TotalActiveDuration")
        {
            var windowEnd = DateTime.UtcNow.ToPeriodStart(60);
            var windowStart = windowEnd.AddHours(-1 * (limit - 1));
            var result = hourLevelFacts.GetDateTimeValues(windowStart, windowEnd, 1, propName);
            return result;
        }

        public static ImmutableArray<DateTimeValue> GetDateTimeValuesByHour(this IEnumerable<AppActivityFact> hourLevelFacts, DateTime start, int limit, string propName = "TotalActiveDuration")
        {
            var windowStart = start;
            var windowEnd = start.AddHours(limit);

            var result = hourLevelFacts.GetDateTimeValues(windowStart, windowEnd, 1, propName);
            return result;
        }

        public static ImmutableArray<DateTimeValue> GetDateTimeValuesByDay(this IEnumerable<AppActivityFact> hourLevelFacts, string propName)
        {
            if (hourLevelFacts.Count() == 0)
            {
                return ImmutableArray.Create<DateTimeValue>();
            }

            var firstInstalled = (from f in hourLevelFacts
                                  select f.DateTime).Min();

            var utcStartTimeOfDay = QueryUtils.GetUtcStartTimeOfDay();
            var rangeStart = firstInstalled.ToDayStart(utcStartTimeOfDay);
            var rangeEnd = DateTime.UtcNow.ToDayStart(utcStartTimeOfDay);

            var result = hourLevelFacts.GetDateTimeValues(rangeStart, rangeEnd, 24, propName);
            return result;
        }

        public static ImmutableArray<DateTimeValue> GetDateTimeValuesByWeek(this IEnumerable<AppActivityFact> hourLevelFacts, string propName = "TotalActiveDuration")
        {
            if (hourLevelFacts.Count() == 0)
            {
                return ImmutableArray.Create<DateTimeValue>();
            }

            var firstUse = (from f in hourLevelFacts
                            select f.DateTime).Min();

            var settings = ClientApplication.Instance.GetSettings();

            var startOfWeek = QueryUtils.GetUtcStartDayOfWeek();

            var startTime = QueryUtils.GetUtcStartTimeOfDay();
            DateTime rangeStart = firstUse.ToWeekStart(startOfWeek, startTime);
            DateTime rangeEnd = DateTime.UtcNow.ToWeekStart(startOfWeek, startTime);

            var result = hourLevelFacts.GetDateTimeValues(rangeStart, rangeEnd, 24 * 7, propName);
            return result;

        }

        static ImmutableArray<DateTimeValue> GetDateTimeValues(this IEnumerable<AppActivityFact> hourLevelFacts, DateTime start, DateTime end, int intervalHours, string propName)
        {
            var getPropValue = GetPropValueFunc(propName);
            var list = new List<DateTimeValue>();
            for (var dateTime = start; dateTime <= end; dateTime = dateTime.AddHours(intervalHours))
            {
                var matchingFacts = hourLevelFacts.Where(fact =>
                {
                    return fact.DateTime >= dateTime && fact.DateTime < dateTime.AddHours(intervalHours);
                });

                var duration = matchingFacts.Sum(getPropValue);
                list.Add(new DateTimeValue
                {
                    DateTime = dateTime,
                    Value = duration
                });
            }
            return list.ToImmutableArray();
        }

        public static Func<AppActivityFact, double> GetPropValueFunc(string propName)
        {
            switch (propName)
            {
                case "TotalActiveDuration":
                    return (item) => item.TotalActiveDuration;
                case "TotalItems":
                    return (item) => item.TotalItems;
                default:
                    throw new NotImplementedException();
            }
        }

    }
}
