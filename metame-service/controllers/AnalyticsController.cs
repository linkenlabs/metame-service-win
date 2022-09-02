using MetaMe.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Web.Http;

namespace MetaMe.WindowsClient.controllers
{
    public class AnalyticsController : ApiController
    {
        private const double MillisecondsToMinutes = 60 * 1000;

        [Route("~/api/analytics/daily/{appName}")]
        [HttpGet]
        public IEnumerable<AnalyticsAppUsageInfo> GetDailyInfo(string appName)
        {
            var hourLevelFacts = ClientApplication.Instance.GetHourLevelFacts();
            var localHourlyPoints = (from item in hourLevelFacts
                                     where item.AppName == appName
                                     select new AnalyticsAppUsageInfo
                                     {
                                         DateTime = item.DateTime,
                                         Duration = item.TotalActiveDuration / MillisecondsToMinutes
                                     }).OrderBy(x => x.DateTime).ToList();
            var utcDayStart = GetUniversalDayStart(DateTime.Now);
            var utcWindowBegin = utcDayStart.AddMonths(-1);
            var allDailyPoints = new List<AnalyticsAppUsageInfo>();
            for (var dateTime = utcWindowBegin; dateTime <= utcDayStart; dateTime = dateTime.AddDays(1))
            {
                var start = dateTime;
                var end = dateTime.AddHours(23);
                var duration = localHourlyPoints
                    .Where(fact => fact.DateTime >= start && fact.DateTime <= end)
                    .Sum(x => x.Duration);
                allDailyPoints.Add(new AnalyticsAppUsageInfo
                {
                    DateTime = dateTime,
                    Duration = duration
                });
            }
            return allDailyPoints;
        }

        [Route("~/api/analytics/hourly/{appName}")]
        [HttpGet]
        public IEnumerable<AnalyticsAppUsageInfo> GetHourlyInfo(string appName)
        {
            var hourLevelFacts = ClientApplication.Instance.GetHourLevelFacts();
            var hourlyPoints = (from item in hourLevelFacts
                                where item.AppName == appName
                                select new AnalyticsAppUsageInfo
                                {
                                    DateTime = item.DateTime,
                                    Duration = item.TotalActiveDuration / MillisecondsToMinutes
                                }).OrderBy(x => x.DateTime).ToList();
            var allHourlyPoints = new List<AnalyticsAppUsageInfo>();
            var last = DateTime.UtcNow;
            last = new DateTime(last.Year, last.Month, last.Day, last.Hour, 0, 0, 0);
            var first = last.AddDays(-7);
            for (var dateTime = first; dateTime <= last; dateTime = dateTime.AddHours(1))
            {
                var duration = 0.0;
                var matchingHourlyPoint = hourlyPoints.Where(fact => fact.DateTime == dateTime);
                if (matchingHourlyPoint.Any())
                {
                    duration = matchingHourlyPoint.First().Duration;
                }
                allHourlyPoints.Add(new AnalyticsAppUsageInfo
                {
                    DateTime = dateTime,
                    Duration = duration
                });
            }
            return allHourlyPoints;
        }

        [Route("~/api/analytics/hourLevelFacts/{appName}")]
        [HttpGet]
        public IEnumerable<DateTimeValue> GetHourLevelFactsByAppName(string appName, int limit = 25)
        {
            var hourLevelFacts = ClientApplication.Instance.GetHourLevelFacts();
            var appHourLevelFacts = (from h in hourLevelFacts
                                     where h.AppName == appName
                                     select h).ToImmutableArray();

            return appHourLevelFacts.GetDateTimeValuesByHour(limit);
        }

        //starts from first day, then gives array summary of every day since for the given application
        [Route("~/api/analytics/dayLevelFacts/{appName}")]
        [HttpGet]
        public IEnumerable<DateTimeValue> GetDayLevelFacts(string appName)
        {
            return QueryUtils.GetAppDayLevelFacts(appName);
        }

        private DateTime GetUniversalDayStart(DateTime localNow)
        {
            var settingsInfo = ClientApplication.Instance.GetSettings();

            var localStartTime = settingsInfo.StartTime.Split(':');
            Int32.TryParse(localStartTime[0], out int localHour);
            Int32.TryParse(localStartTime[1], out int localMinute);
            var localDayStart = new DateTime(localNow.Year, localNow.Month, localNow.Day, localHour, localMinute, 0, 0);
            if (localDayStart > localNow)
            {
                localDayStart = localDayStart.AddDays(-1);
            }
            var utcDayStart = localDayStart.ToUniversalTime();
            return utcDayStart;
        }

        [Route("~/api/analytics/productivity-overview/{granularity}/{page}")]
        [HttpGet]
        public AnalyticsProductivityOverview GetProductivityOverview(string granularity, int page)
        {
            var hourLevelFacts = ClientApplication.Instance.GetHourLevelFacts();
            var productiveGroup = (from g in ClientApplication.Instance.GetGroups()
                                   where g.Name == "Productive"
                                   select g).First();

            var productiveAppNames = productiveGroup.Items;

            var settingsInfo = ClientApplication.Instance.GetSettings();
            var hourSetting = Int32.Parse(settingsInfo.StartTime.Split(':')[0]);
            var localDateTimeNow = DateTime.Now;
            var localDateTimeTranslated = localDateTimeNow.AddHours(-hourSetting);
            var localDateTimeStart = new DateTime(localDateTimeTranslated.Year, localDateTimeTranslated.Month, localDateTimeTranslated.Day, hourSetting, 0, 0, 0);

            string datesFooter;
            var totalProductiveTimeSeries = new List<DateTimeValue>();
            var totalActiveTimeSeries = new List<DateTimeValue>();

            // Calculate local date ranges
            if (granularity == "day")
            {
                var localDateTimeLower = localDateTimeStart.AddDays(-page);
                var localDateTimeUpper = localDateTimeLower.AddDays(1).AddHours(-1);
                datesFooter = localDateTimeLower.ToString("d MMMM");
                var utcDateTimeLower = localDateTimeLower.ToUniversalTime();
                var utcDateTimeUpper = localDateTimeUpper.ToUniversalTime();
                var relevantHourLevelFacts = (from fact in hourLevelFacts
                                              where fact.DateTime >= utcDateTimeLower && fact.DateTime <= utcDateTimeUpper
                                              select fact).ToList();
                for (var dateTime = utcDateTimeLower; dateTime <= utcDateTimeUpper; dateTime = dateTime.AddHours(1))
                {
                    var factsThisHour = relevantHourLevelFacts.Where(fact => fact.DateTime == dateTime);
                    var totalActiveDuration = (from f in factsThisHour
                                               select f).Sum(fact => fact.TotalActiveDuration);
                    var productiveDuration = (from f in factsThisHour
                                              where productiveAppNames.Contains(f.AppName)
                                              select f).Sum(fact => fact.TotalActiveDuration);
                    totalActiveTimeSeries.Add(new DateTimeValue
                    {
                        DateTime = dateTime,
                        Value = totalActiveDuration
                    });
                    totalProductiveTimeSeries.Add(new DateTimeValue
                    {
                        DateTime = dateTime,
                        Value = productiveDuration
                    });
                }
            }
            else // granularity == "month"
            {
                var localDateTimeLower = localDateTimeStart.AddMonths(-page).AddDays(-localDateTimeStart.Day + 1);
                var localDateTimeUpper = localDateTimeLower.AddMonths(1).AddHours(-1);
                datesFooter = localDateTimeLower.ToString("MMMM yyyy");
                var utcDateTimeLower = localDateTimeLower.ToUniversalTime();
                var utcDateTimeUpper = localDateTimeUpper.ToUniversalTime();
                var relevantHourLevelFacts = (from fact in hourLevelFacts
                                              where fact.DateTime >= utcDateTimeLower && fact.DateTime <= utcDateTimeUpper
                                              select fact).ToList();
                for (var dateTime = utcDateTimeLower; dateTime <= utcDateTimeUpper; dateTime = dateTime.AddDays(1))
                {
                    var factsThisDay = relevantHourLevelFacts.Where(fact => fact.DateTime >= dateTime && fact.DateTime <= dateTime.AddDays(1).AddHours(-1));
                    var totalActiveDuration = (from f in factsThisDay
                                               select f).Sum(fact => fact.TotalActiveDuration);
                    var productiveDuration = (from f in factsThisDay
                                              where productiveAppNames.Contains(f.AppName)
                                              select f).Sum(fact => fact.TotalActiveDuration);
                    totalActiveTimeSeries.Add(new DateTimeValue
                    {
                        DateTime = dateTime,
                        Value = totalActiveDuration
                    });
                    totalProductiveTimeSeries.Add(new DateTimeValue
                    {
                        DateTime = dateTime,
                        Value = productiveDuration
                    });
                }
            }

            return new AnalyticsProductivityOverview
            {
                DatesFooter = datesFooter,
                TotalActiveTimeSeries = totalActiveTimeSeries.ToImmutableArray(),
                TotalProductiveTimeSeries = totalProductiveTimeSeries.ToImmutableArray()
            };
        }
        [Route("~/api/analytics/most-used-apps")]
        [HttpGet]
        public ImmutableArray<NameValueGroup> GetMostUsedApps(DateTime start, DateTime end, int limit = 25, string group = "")
        {
            if (start.Kind == DateTimeKind.Local)
            {
                start = start.ToUniversalTime();
            }

            if (end.Kind == DateTimeKind.Local)
            {
                end = end.ToUniversalTime();
            }

            var appActivityFacts = QueryUtils.GetMostUsedApps(start, end);
            var hiddenApps = ClientApplication.Instance.GetHiddenAppList();
            var nameValues = (from item in appActivityFacts
                              where !hiddenApps.Items.Contains(item.AppName)
                              select new NameValue
                              {
                                  Name = item.AppName,
                                  Value = item.TotalActiveDuration
                              }).OrderByDescending(d => d.Value).ToList();


            //apply group filter
            if (!string.IsNullOrEmpty(group))
            {
                var groupMembershipFunc = QueryUtils.GetGroupMembershipFunc(group, hiddenApps.Items);
                nameValues = nameValues.FindAll((nv) => groupMembershipFunc(nv.Name));
            }



            //apply limit filter
            if (limit > 0)
            {
                nameValues = nameValues.Take(Math.Min(nameValues.Count, limit)).ToList();
            }

            //groups
            var groups = ClientApplication.Instance.GetGroups();

            var converted = nameValues.Select((datum) =>
            {
                var firstGroup = (from g in groups

                                  where g.Items.Contains(datum.Name)
                                  select g).FirstOrDefault();

                return new NameValueGroup
                {
                    Name = datum.Name,
                    Value = datum.Value,
                    Group = firstGroup != null ? firstGroup.Name : String.Empty
                };
            }).ToImmutableArray();

            return converted;
        }

    }
}
