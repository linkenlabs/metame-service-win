using log4net;
using MetaMe.Core;
using MetaMe.WindowsClient.controllers;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace MetaMe.WindowsClient
{
    class CandleUtils
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        const string TOTAL_SERIES_NAME = "Total";

        public static CandleSeries ConvertToCandleSeries(string groupName, int count, int periodMinutes, ImmutableArray<DateTimeValue> previousValues, ImmutableArray<AppActivityFact> newFacts, AppActivityFactGeneratorState state, string[] hiddenApps)
        {
            var periodStart = DateTime.UtcNow.ToPeriodStart(periodMinutes);
            var dataSeries = DateTimeValue.RangeFromEnd(periodStart, count, periodMinutes);

            var relevantFacts = newFacts.FilterByGroup(groupName);

            //filter out hidden apps
            relevantFacts = (from f in relevantFacts
                             where !hiddenApps.Contains(f.AppName)
                             select f).ToImmutableArray();

            var newDateTimeValues = relevantFacts.GetDateTimeValues();

            TimeVector vector = CalculateTimeVector(groupName, state, periodMinutes, hiddenApps);
            var vectorValues = ConvertToDateTimeValues(vector);

            dataSeries = dataSeries
               .UpdateWith(previousValues)
               .UpdateWith(newDateTimeValues)
               .UpdateWith(vectorValues);

            //trim values if they exceed
            var overflowed = (from d in dataSeries
                              where d.Value > periodMinutes * 60 * 1000
                              select d).ToList();

            if (overflowed.Count > 0)
            {
                log.Warn("Candle overflow detected...");
                overflowed.ForEach(item =>
                {
                    item.Value = periodMinutes * 60 * 1000;
                });
            }

            return new CandleSeries
            {
                DataSeries = dataSeries,
                IsActive = vector.IsActive,
                LastUpdate = vector.Timestamp,
                Name = groupName,
                Type = "Group"
            };
        }

        static TimeVector CalculateTimeVector(string groupName, AppActivityFactGeneratorState state, int periodMinutes, string[] hiddenApps)
        {
            if (state.CurrentActivity == null)
            {
                return new TimeVector
                {
                    PeriodStart = state.CurrentPeriod,
                    IsActive = false,
                    Timestamp = state.CurrentPeriod,
                    Value = 0
                };
            }

            if (groupName == TOTAL_SERIES_NAME)
            {
                //total should not be active if its a hidden app
                return CalculateTotalTimeVector(state, periodMinutes, hiddenApps);
            }

            //Select all apps of this group then sum
            var matchingGroup = (from g in ClientApplication.Instance.GetGroups()
                                 where g.Name == groupName
                                 select g).First();

            var appActivityFacts = AppActivityFactUtils.CalculateAppActivityFacts(state.ActivitiesBuffer, state.IdleActivitiesBuffer, periodMinutes).ToImmutableArray();
            var relevantFacts = appActivityFacts.Where(f => matchingGroup.Items.Contains(f.AppName) && f.DateTime == state.CurrentPeriod).ToImmutableArray();

            if (!matchingGroup.Items.Contains(state.CurrentActivity.AppName))
            {
                return new TimeVector
                {
                    PeriodStart = state.CurrentPeriod,
                    IsActive = false,
                    Timestamp = state.CurrentActivity.Timestamp,
                    Value = relevantFacts.Sum(d => d.TotalActiveDuration)
                };
            }

            //Calculate the diff after the current activity taking into account idle time since
            var idleActivityAfterCurrentActivity = state.IdleActivitiesBuffer.Intersect(new DateRange
            {
                Start = state.CurrentActivity.Timestamp,
                Stop = DateTime.MaxValue
            });

            var timeStamp = state.CurrentActivity.Timestamp;
            if (idleActivityAfterCurrentActivity.Length > 0)
            {
                //if there is idle activity, adjust lastUpdate to the last time an idle timer stopped in this activity
                timeStamp = (from item in idleActivityAfterCurrentActivity
                             select item.Stop).Max();
            }

            if (state.CurrentIdleEvent.State == Sensors.IdleStateEnum.Idle
                && state.CurrentIdleEvent.Timestamp > state.CurrentActivity.Timestamp)
            {
                timeStamp = state.CurrentIdleEvent.Timestamp;
            }

            if (timeStamp < state.CurrentActivity.Timestamp)
            {
                log.Debug("Something wrong");
            }

            var isActive = state.CurrentIdleEvent.State == Sensors.IdleStateEnum.Active;
            var currentActivityStartToLastUpdate = timeStamp.Subtract(state.CurrentActivity.Timestamp).TotalMilliseconds;
            var idleTimeSinceCurrentActivityStart = idleActivityAfterCurrentActivity.Length == 0 ? 0 : idleActivityAfterCurrentActivity.Sum(item => item.GetDuration());
            var activeDurationSinceCurrentActivityStart = currentActivityStartToLastUpdate - idleTimeSinceCurrentActivityStart;
            var value = relevantFacts.Sum(item => item.TotalActiveDuration) + activeDurationSinceCurrentActivityStart;

            if (Math.Abs(value) > periodMinutes * 60 * 1000)
            {
                log.Warn("Value exceeds period size");
            }

            return new TimeVector
            {
                PeriodStart = state.CurrentPeriod,
                IsActive = isActive,
                Timestamp = timeStamp,
                Value = value
            };
        }

        static TimeVector CalculateTotalTimeVector(AppActivityFactGeneratorState state, int periodMinutes, string[] hiddenApps)
        {
            var periodStart = state.CurrentPeriod;
            var timeStamp = state.CurrentActivity.Timestamp;

            var idleActivitySinceCurrentActivityStart = state.IdleActivitiesBuffer.Subtract(new DateRange
            {
                Start = DateTime.MinValue,
                Stop = DateTime.MaxValue
            });

            if (idleActivitySinceCurrentActivityStart.Length > 0)
            {
                //if there is idle activity, adjust lastUpdate to the last time an idle timer stopped in this activity
                timeStamp = (from item in idleActivitySinceCurrentActivityStart
                             select item.Stop).Max();
            }

            if (state.CurrentIdleEvent.State == Sensors.IdleStateEnum.Idle
                && state.CurrentIdleEvent.Timestamp > timeStamp)
            {
                timeStamp = state.CurrentIdleEvent.Timestamp;
            }


            var relevantFacts = AppActivityFactUtils.CalculateAppActivityFacts(state.ActivitiesBuffer, state.IdleActivitiesBuffer, periodMinutes).ToImmutableArray();

            //remove hidden
            relevantFacts = (from f in relevantFacts
                             where !hiddenApps.Contains(f.AppName)
                             select f).ToImmutableArray();

            //should be in the same hour but make the filter just in case
            relevantFacts = (from i in relevantFacts
                             where i.DateTime == periodStart
                             select i).ToImmutableArray();

            //if its a hidden app no need to account for activeduration since it started
            if (hiddenApps.Contains(state.CurrentActivity.AppName))
            {
                return new TimeVector
                {
                    PeriodStart = periodStart,
                    IsActive = false,
                    Timestamp = timeStamp,
                    Value = relevantFacts.Sum(d => d.TotalActiveDuration)
                };
            }

            var isActive = state.CurrentIdleEvent.State == Sensors.IdleStateEnum.Active;
            var currentActivityStartToLastUpdate = timeStamp.Subtract(state.CurrentActivity.Timestamp).TotalMilliseconds;
            var idleTimeSinceCurrentActivityStart = idleActivitySinceCurrentActivityStart.Length == 0 ? 0 : idleActivitySinceCurrentActivityStart.Sum(item => item.GetDuration());
            var activeDurationSinceCurrentActivityStart = currentActivityStartToLastUpdate - idleTimeSinceCurrentActivityStart;
            var value = relevantFacts.Sum(item => item.TotalActiveDuration) + activeDurationSinceCurrentActivityStart;

            if (value > timeStamp.Subtract(periodStart).TotalMilliseconds)
            {
                log.Warn("Value exceeding time elapsed in the hour...");
            }

            return new TimeVector
            {
                PeriodStart = periodStart,
                IsActive = isActive,
                Timestamp = timeStamp,
                Value = value
            };
        }

        static ImmutableArray<DateTimeValue> ConvertToDateTimeValues(TimeVector vector)
        {
            var dateTimeValues = ImmutableArray.Create(new DateTimeValue
            {
                DateTime = vector.PeriodStart,
                Value = vector.Value
            });

            return dateTimeValues;
        }

    }
}
