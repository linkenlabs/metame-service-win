using MetaMe.Core;
using MetaMe.Sensors;
using MetaMe.WindowsClient.controllers;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace MetaMe.WindowsClient
{
    class QueryUtils
    {
        public static Group GetByName(string name)
        {
            Group data = (from g in ClientApplication.Instance.GetGroups()
                          where g.Name == name
                          select g).FirstOrDefault();
            return data;
        }

        public static Func<string, bool> GetGroupMembershipFunc(string groupName, string[] hiddenApps)
        {
            if (groupName == "Total")
            {
                return (appName) =>
                {
                    return !hiddenApps.Contains(appName);
                };
            }
            else if (groupName == "Neutral")
            {
                var productiveGroup = GetByName("Productive");
                var unproductiveGroup = GetByName("Unproductive");

                return (appName) =>
                {
                    if (hiddenApps.Contains(appName))
                    {
                        return false;
                    }
                    return !productiveGroup.Items.Contains(appName)
                    && !unproductiveGroup.Items.Contains(appName);
                };
            }
            else
            {
                var group = GetByName(groupName);
                if (group == null)
                {
                    throw new NotImplementedException();
                }
                return (appName) =>
                {
                    if (hiddenApps.Contains(appName))
                    {
                        return false;
                    }

                    return group.Items.Contains(appName);
                };
            }
        }

        public static ImmutableArray<AppActivityFact> GetMostUsedApps(DateTime start, DateTime end)
        {
            if (start.Kind == DateTimeKind.Local)
            {
                start = start.ToUniversalTime();
            }

            if (end.Kind == DateTimeKind.Local)
            {
                end = start.ToUniversalTime();
            }

            //get all the activities and idle activities within range
            var relevantActivities = (from item in ClientApplication.Instance.GetAppActivityInfo().SkipTill(start)
                                      where item.Stop > start && item.Stop <= end
                                      || item.Start >= start && item.Start < end
                                      || item.Start < start && item.Stop > end
                                      select item).ToImmutableArray();

            var relevantIdleActivities = (from item in ClientApplication.Instance.GetIdleActivityInfo().SkipTill(start)
                                          where item.Stop > start && item.Stop <= end
                                          || item.Start >= start && item.Start < end
                                          || item.Start < start && item.Stop > end
                                          select item).ToImmutableArray();

            var utcNow = DateTime.UtcNow;
            //append current appActivity and idleEvent
            var currentActivity = ClientApplication.Instance.GetCurrentAppActivityEvent();
            if (currentActivity != null)
            {
                relevantActivities = relevantActivities.Add(new ProcessActivityInfo
                {
                    AppName = currentActivity.AppName,
                    Start = currentActivity.Timestamp,
                    Stop = utcNow
                });
            }

            var currentIdleActivity = ClientApplication.Instance.GetCurrentIdleEvent();

            if (currentIdleActivity != null
                && currentIdleActivity.State == IdleStateEnum.Idle)
            {
                relevantIdleActivities = relevantIdleActivities.Add(new IdleActivityInfo
                {
                    Type = null,
                    Start = currentIdleActivity.Timestamp,
                    Stop = DateTime.MaxValue
                });
            }

            var range = new DateRange
            {
                Start = start,
                Stop = end
            };

            relevantActivities = relevantActivities.Intersect(range);
            relevantIdleActivities = relevantIdleActivities.Intersect(range);
            var appActivityFacts = AppActivityFactUtils.CalculateAppActivityFacts2(start, relevantActivities, relevantIdleActivities);
            return appActivityFacts.ToImmutableArray();
        }

        public static ImmutableArray<DateTimeValue>[] GetGroupSeries2(string[] groupNames, DateTime start, int interval, int limit)
        {
            //convert if not utc
            if (start.Kind == DateTimeKind.Local)
            {
                start = start.ToUniversalTime();
            }
            DateTime end = start.AddMinutes(interval * limit);

            //get all the activities and idle activities within range
            var relevantActivities = (from item in ClientApplication.Instance.GetAppActivityInfo().SkipTill(start)
                                      where item.Stop > start && item.Stop <= end
                                      || item.Start >= start && item.Start < end
                                      || item.Start < start && item.Stop > end
                                      select item).ToImmutableArray();

            var relevantIdleActivities = (from item in ClientApplication.Instance.GetIdleActivityInfo().SkipTill(start)
                                          where item.Stop > start && item.Stop <= end
                                          || item.Start >= start && item.Start < end
                                          || item.Start < start && item.Stop > end
                                          select item).ToImmutableArray();

            //relabel
            relevantActivities = RelabelAsGroupName(relevantActivities);

            //merge
            relevantActivities = ApplyIdle(relevantActivities, relevantIdleActivities);

            //remove hidden
            relevantActivities = relevantActivities.Where(d => d.AppName != "Hidden").ToImmutableArray();

            //now chunk
            var chunksGrouped = Chunk(relevantActivities, start, interval);

            List<ImmutableArray<DateTimeValue>> list = new List<ImmutableArray<DateTimeValue>>();
            //return datetime values for each group required
            foreach (var groupName in groupNames)
            {
                //now create the dateTimeValues
                var dateTimeValues = Enumerable.Range(0, limit).ToList().ConvertAll((index) =>
                {
                    var dateStart = start.AddMinutes(index * interval);
                    var matchingChunk = chunksGrouped.Where(datum => datum.Item1 == index).FirstOrDefault();

                    double value = 0;
                    if (matchingChunk != null)
                    {
                        if (groupName == "Total")
                        {
                            value = matchingChunk.Item2.Sum(d => d.GetDuration());
                        }
                        else
                        {
                            value = matchingChunk.Item2.Where(d => d.AppName == groupName).Sum(d => d.GetDuration());
                        }
                    }

                    return new DateTimeValue
                    {
                        DateTime = dateStart,
                        Value = value
                    };
                });

                list.Add(dateTimeValues.ToImmutableArray());
            }
            return list.ToArray();
        }
        public static ImmutableArray<DateTimeValue>[] GetAppSeries(string[] appNames, DateTime start, int interval, int limit)
        {
            //convert if not utc
            if (start.Kind == DateTimeKind.Local)
            {
                start = start.ToUniversalTime();
            }

            DateTime end = start.AddMinutes(interval * limit);
            if (limit == 0)
            {
                end = DateTime.UtcNow.ToPeriodEnd(interval);
            }

            //get all the activities and idle activities within range
            var relevantActivities = (from item in ClientApplication.Instance.GetAppActivityInfo().SkipTill(start)
                                      where item.Stop > start && item.Stop <= end
                                      || item.Start >= start && item.Start < end
                                      || item.Start < start && item.Stop > end
                                      select item).ToImmutableArray();

            var relevantIdleActivities = (from item in ClientApplication.Instance.GetIdleActivityInfo().SkipTill(start)
                                          where item.Stop > start && item.Stop <= end
                                          || item.Start >= start && item.Start < end
                                          || item.Start < start && item.Stop > end
                                          select item).ToImmutableArray();


            relevantActivities = ApplyIdle(relevantActivities, relevantIdleActivities);

            var hiddenAppNames = ClientApplication.Instance.GetHiddenAppList().Items;

            //remove hidden
            relevantActivities = relevantActivities.Where(d => !hiddenAppNames.Contains(d.AppName)).ToImmutableArray();

            //now chunk
            var chunksGrouped = Chunk(relevantActivities, start, interval);

            List<ImmutableArray<DateTimeValue>> list = new List<ImmutableArray<DateTimeValue>>();

            int seriesLength = limit;
            if (limit == 0)
            {
                seriesLength = Convert.ToInt32(end.Subtract(start).TotalMinutes) / interval;
            }

            foreach (var appName in appNames)
            {
                //get matching chunks
                var dateTimeValues = Enumerable.Range(0, seriesLength).ToList().ConvertAll((index) =>
                {
                    var dateStart = start.AddMinutes(index * interval);
                    var matchingChunk = chunksGrouped.Where(datum => datum.Item1 == index).FirstOrDefault();

                    if (matchingChunk == null)
                    {
                        return new DateTimeValue
                        {
                            DateTime = dateStart,
                            Value = 0
                        };
                    }

                    double value = matchingChunk.Item2.Where(d => d.AppName == appName).Sum(d => d.GetDuration());

                    return new DateTimeValue
                    {
                        DateTime = dateStart,
                        Value = value
                    };
                });

                list.Add(dateTimeValues.ToImmutableArray());
            }

            return list.ToArray();
        }

        public static ImmutableArray<string> GetAppNames(DateTime start, DateTime end, bool showHidden)
        {
            var relevantActivities = (from item in ClientApplication.Instance.GetAppActivityInfo().SkipTill(start)
                                      where item.Stop > start && item.Stop <= end
                                      || item.Start >= start && item.Start < end
                                      || item.Start < start && item.Stop > end
                                      select item).ToImmutableArray();

            var relevantIdleActivities = (from item in ClientApplication.Instance.GetIdleActivityInfo().SkipTill(start)
                                          where item.Stop > start && item.Stop <= end
                                          || item.Start >= start && item.Start < end
                                          || item.Start < start && item.Stop > end
                                          select item).ToImmutableArray();

            //merge
            relevantActivities = ApplyIdle(relevantActivities, relevantIdleActivities);

            //remove hidden
            if (!showHidden)
            {
                var hiddenAppNames = ClientApplication.Instance.GetHiddenAppList().Items;
                relevantActivities = relevantActivities.Where(d => !hiddenAppNames.Contains(d.AppName)).ToImmutableArray();
            }

            //need to refilter since idle might have chopped some irrelevant
            var appNames = (from item in relevantActivities
                            where item.Stop > start && item.Stop <= end
                            || item.Start >= start && item.Start < end
                            || item.Start < start && item.Stop > end
                            select item.AppName).Distinct();

            return appNames.ToImmutableArray();
        }

        //chops appActivity in pieces, group by index
        static Tuple<int, ProcessActivityInfo[]>[] Chunk(ImmutableArray<ProcessActivityInfo> activities, DateTime start, int intervalMins)
        {
            Dictionary<int, ImmutableArray<ProcessActivityInfo>> dictionary = new Dictionary<int, ImmutableArray<ProcessActivityInfo>>();

            foreach (var item in activities)
            {
                var chunks = Chunk(item, start, intervalMins);

                foreach (var chunk in chunks)
                {
                    if (!dictionary.ContainsKey(chunk.Item1))
                    {
                        dictionary.Add(chunk.Item1, ImmutableArray.Create<ProcessActivityInfo>());
                    }
                    dictionary[chunk.Item1] = dictionary[chunk.Item1].Add(chunk.Item2);
                }
            }

            List<Tuple<int, ProcessActivityInfo[]>> list = new List<Tuple<int, ProcessActivityInfo[]>>();

            foreach (var key in dictionary.Keys)
            {
                list.Add(new Tuple<int, ProcessActivityInfo[]>(key, dictionary[key].ToArray()));
            }

            return list.ToArray();
        }

        static Tuple<int, ProcessActivityInfo>[] Chunk(ProcessActivityInfo info, DateTime start, int intervalMins)
        {
            var startIndex = Convert.ToInt32(Math.Floor((info.Start - start).TotalMinutes / intervalMins));
            var stopIndex = Convert.ToInt32(Math.Floor((info.Stop - start).TotalMinutes / intervalMins));

            //if stop < 0 then do nothing
            if (stopIndex < 0)
            {
                return new Tuple<int, ProcessActivityInfo>[0];
            }

            List<Tuple<int, ProcessActivityInfo>> list = new List<Tuple<int, ProcessActivityInfo>>();
            for (int i = Math.Max(startIndex, 0); i <= stopIndex; i++)
            {
                var tempStart = start.AddMinutes(i * intervalMins);
                var tempStop = start.AddMinutes((i + 1) * intervalMins);
                //get intersection

                var portion = info.Intersect(new DateRange { Start = tempStart, Stop = tempStop });
                list.Add(new Tuple<int, ProcessActivityInfo>(i, portion));
            }
            return list.ToArray();
        }

        static ImmutableArray<ProcessActivityInfo> ApplyIdle(ImmutableArray<ProcessActivityInfo> appActivities, ImmutableArray<IdleActivityInfo> idleActivities)
        {
            IdleTimeMerger merger = new IdleTimeMerger(appActivities, idleActivities);
            var result = merger.PullAll();
            return result;
        }

        static ImmutableArray<ProcessActivityInfo> RelabelAsGroupName(ImmutableArray<ProcessActivityInfo> appActivities)
        {
            var hiddenApps = ClientApplication.Instance.GetHiddenAppList();

            var groups = ClientApplication.Instance.GetGroups();
            var productiveGroup = groups.Where(g => g.Name == "Productive").First();
            var unproductiveGroup = groups.Where(g => g.Name == "Unproductive").First();

            var productiveHashset = productiveGroup.Items.ToHashSet();
            var unproductiveHashset = unproductiveGroup.Items.ToHashSet();
            var hiddenHashset = hiddenApps.Items.ToHashSet();

            Func<string, string> labelFunc = (appName) =>
             {
                 if (hiddenHashset.Contains(appName))
                 {
                     return "Hidden";
                 }
                 if (productiveHashset.Contains(appName))
                 {
                     return "Productive";
                 }
                 else if (unproductiveHashset.Contains(appName))
                 {
                     return "Unproductive";
                 }
                 else
                 {
                     return "Neutral";
                 }
             };

            List<ProcessActivityInfo> converted = new List<ProcessActivityInfo>();
            foreach (var item in appActivities)
            {
                converted.Add(new ProcessActivityInfo
                {
                    AppName = labelFunc(item.AppName),
                    Start = item.Start,
                    Stop = item.Stop
                });
            }
            return converted.ToImmutableArray();

        }

        public static ImmutableArray<DateTimeValue>[] GetGroupSeries(string[] groupNames, DateTime start, int interval, int limit)
        {
            //convert if not utc
            if (start.Kind == DateTimeKind.Local)
            {
                start = start.ToUniversalTime();
            }
            DateTime end = start.AddMinutes(interval * limit);

            //get all the activities and idle activities within range
            var relevantActivities = (from item in ClientApplication.Instance.GetAppActivityInfo().SkipTill(start)
                                      where item.Stop > start && item.Stop <= end
                                      || item.Start >= start && item.Start < end
                                      || item.Start < start && item.Stop > end
                                      select item).ToImmutableArray();

            var relevantIdleActivities = (from item in ClientApplication.Instance.GetIdleActivityInfo().SkipTill(start)
                                          where item.Stop > start && item.Stop <= end
                                          || item.Start >= start && item.Start < end
                                          || item.Start < start && item.Stop > end
                                          select item).ToImmutableArray();

            var range = new DateRange
            {
                Start = start,
                Stop = end
            };

            relevantActivities = relevantActivities.Intersect(range);
            relevantIdleActivities = relevantIdleActivities.Intersect(range);

            //now calculate
            var result = AppActivityFactUtils.CalculateNextState(start, relevantActivities, relevantIdleActivities, interval);


            //now summarize the last AppActivityFact
            var appActivityFacts = AppActivityFactUtils.CalculateAppActivityFacts2(result.Item1.CurrentPeriod, result.Item1.ActivitiesBuffer, result.Item1.IdleActivitiesBuffer);

            var allFacts = result.Item2.AddRange(appActivityFacts);
            var hiddenApps = ClientApplication.Instance.GetHiddenAppList().Items;

            List<ImmutableArray<DateTimeValue>> list = new List<ImmutableArray<DateTimeValue>>();
            foreach (var groupName in groupNames)
            {
                var isMemberFunc = GetGroupMembershipFunc(groupName, hiddenApps);
                var relevantFacts = (from f in allFacts
                                     where isMemberFunc(f.AppName)
                                     select f).ToImmutableArray();
                //
                var dateTimeValues = relevantFacts.GetDateTimeValues2(start, interval, limit);
                list.Add(dateTimeValues);
            }

            return list.ToArray();
        }

        public static ImmutableArray<DateTimeValue> GetGroupHourLevelFacts(string groupName, DateTime start, int limit, string propName = "TotalActiveDuration")
        {
            //convert if not utc
            if (start.Kind == DateTimeKind.Local)
            {
                start = start.ToUniversalTime().ToPeriodStart(60);
            }

            DateTime end = start.AddHours(limit);

            Group groupItem = GetByName(groupName);

            var hourLevelFacts = ClientApplication.Instance.GetHourLevelFacts();

            var relevantHourLevelFacts = (from f in hourLevelFacts
                                          where
                                          groupItem.Items.Contains(f.AppName)
                                          && f.DateTime >= start && f.DateTime < end
                                          select f);

            return relevantHourLevelFacts.GetDateTimeValuesByHour(start, limit, propName);
        }

        public static ImmutableArray<DateTimeValue> GetGroupDayLevelFacts(string groupName, string propName = "TotalActiveDuration")
        {
            Group group = GetByName(groupName);
            if (group == null)
            {
                return ImmutableArray.Create<DateTimeValue>();
            }

            string[] groupItems = group.Items;
            var hourLevelFacts = ClientApplication.Instance.GetHourLevelFacts();
            var relevantHourLevelFacts = (from f in hourLevelFacts
                                          where groupItems.Contains(f.AppName)
                                          select f).ToArray();

            return relevantHourLevelFacts.GetDateTimeValuesByDay(propName);
        }

        public static ImmutableArray<DateTimeValue> GeGroupWeekLevelFacts(string name, string propName = "TotalActiveDuration")
        {
            Group group = GetByName(name);
            if (group == null)
            {
                return ImmutableArray.Create<DateTimeValue>();
            }

            string[] groupItems = group.Items;
            var hourLevelFacts = ClientApplication.Instance.GetHourLevelFacts();
            var relevantHourLevelFacts = (from f in hourLevelFacts
                                          where groupItems.Contains(f.AppName)
                                          select f).ToArray();

            return relevantHourLevelFacts.GetDateTimeValuesByWeek(propName);
        }

        public static ImmutableArray<DateTimeValue> GetAppHourLevelFacts(string appName, DateTime start, int limit, string propName = "TotalActiveDuration")
        {
            //convert if not utc
            if (start.Kind == DateTimeKind.Local)
            {
                start = start.ToUniversalTime().ToPeriodStart(60);
            }

            DateTime end = start.AddHours(limit);

            var hourLevelFacts = ClientApplication.Instance.GetHourLevelFacts();

            var relevantHourLevelFacts = (from f in hourLevelFacts
                                          where f.AppName == appName
                                          && f.DateTime >= start && f.DateTime < end
                                          select f).ToImmutableArray();

            return relevantHourLevelFacts.GetDateTimeValuesByHour(start, limit, propName);
        }

        public static ImmutableArray<DateTimeValue> GetAppDayLevelFacts(string appName, string propName = "TotalActiveDuration")
        {
            var hourLevelFacts = ClientApplication.Instance.GetHourLevelFacts();

            var appHourLevelFacts = (from f in hourLevelFacts
                                     where f.AppName == appName
                                     select f).ToImmutableArray();

            return appHourLevelFacts.GetDateTimeValuesByDay(propName);
        }

        public static ImmutableArray<DateTimeValue> GetAppWeekLevelFacts(string name, string propName = "TotalActiveDuration")
        {
            var hourLevelFacts = ClientApplication.Instance.GetHourLevelFacts();
            var appHourLevelFacts = (from f in hourLevelFacts
                                     where f.AppName == name
                                     select f).ToImmutableArray();

            return appHourLevelFacts.GetDateTimeValuesByWeek(propName);
        }

        public static ImmutableArray<DateTimeValue> GetDayLevelFacts(string type, string name)
        {
            switch (type)
            {
                case "group":
                    return GetGroupDayLevelFacts(name);
                case "app":
                    return GetAppDayLevelFacts(name);
                default:
                    throw new NotImplementedException();
            }
        }

        public static DayOfWeek GetUtcStartDayOfWeek()
        {
            var settings = ClientApplication.Instance.GetSettings();
            int hours = Convert.ToInt32(settings.StartTime.Split(':')[0]);


            if (!Enum.TryParse(settings.StartDay, out DayOfWeek startDayOfWeek))
            {
                throw new NotImplementedException();
            }

            DateTime localizedStartTime = DateTime.Now.Date.AddHours(hours);
            DateTime utcStartTime = localizedStartTime.ToUniversalTime();

            var dateDifference = Convert.ToInt32(utcStartTime.Date.Subtract(localizedStartTime.Date).TotalDays);

            int diff = (7 + ((int)startDayOfWeek + dateDifference)) % 7;

            return (DayOfWeek)diff;
        }

        public static TimeSpan GetUtcStartTimeOfDay()
        {
            var settingsInfo = ClientApplication.Instance.GetSettings();

            //settingsInfo.StartTime
            int hours = Convert.ToInt32(settingsInfo.StartTime.Split(':')[0]);

            //find startTime in UTC
            DateTime localizedStartTime = DateTime.Now.Date.AddHours(hours);
            DateTime utcStartTime = localizedStartTime.ToUniversalTime();
            return utcStartTime.TimeOfDay;
        }
    }
}
