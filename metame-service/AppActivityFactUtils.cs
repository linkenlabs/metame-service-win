using log4net;
using MetaMe.Core;
using MetaMe.Sensors;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;

namespace MetaMe.WindowsClient
{
    class AppActivityFactUtils
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public static DateTime GetPeriodStart(ImmutableArray<ProcessActivityInfo> activities, int periodMinutes)
        {
            var start = DateTime.MinValue;

            if (activities.Length > 0)
            {
                //use minimum value
                var minDate = (from a in activities
                               select a.Start).Min();

                start = minDate.ToPeriodStart(periodMinutes);
            }

            return start;
        }
        public static Tuple<AppActivityFactGeneratorState, ImmutableArray<AppActivityFact>> CalculateNextState(AppActivityFactGeneratorState state, AppActivityFactGeneratorEvent item, int periodMinutes)
        {
            if (item.AppActivityEvent != null)
            {
                return CalculateNextState(state, item.AppActivityEvent, periodMinutes);
            }
            if (item.IdleEvent != null)
            {
                return CalculateNextState(state, item.IdleEvent);
            }
            else
            {
                throw new NotImplementedException();
            }
        }
        //handle AppActivityEvent
        static Tuple<AppActivityFactGeneratorState, ImmutableArray<AppActivityFact>> CalculateNextState(AppActivityFactGeneratorState state, AppActivityEvent item, int periodMinutes)
        {
            if (item == null)
            {
                throw new Exception("AppActivityEvent is null");
            }

            //item is not in current period
            if (item.Timestamp < state.CurrentPeriod)
            {
                log.WarnFormat("Event timestamp: {0} < current period: {1}", item.Timestamp, state.CurrentPeriod);
                return Tuple.Create(state, ImmutableArray.Create<AppActivityFact>());
            }

            //handle turn off case
            if (item.AppName == null)
            {
                return Tuple.Create(new AppActivityFactGeneratorState
                {
                    CurrentPeriod = state.CurrentPeriod,
                    CurrentActivity = item,
                    CurrentIdleEvent = state.CurrentIdleEvent,
                    ActivitiesBuffer = state.ActivitiesBuffer.Add(new ProcessActivityInfo
                    {
                        AppName = state.CurrentActivity.AppName,
                        Start = state.CurrentActivity.Timestamp,
                        Stop = item.Timestamp
                    }),
                    IdleActivitiesBuffer = state.IdleActivitiesBuffer
                }, ImmutableArray.Create<AppActivityFact>());
            }
            //skip to current period if first run
            DateTime currentPeriod = state.CurrentPeriod == DateTime.MinValue ? item.Timestamp.ToPeriodStart(periodMinutes)
                : state.CurrentPeriod;

            IdleEvent nextIdleEvent = state.CurrentIdleEvent ?? new IdleEvent
            {
                State = IdleStateEnum.Active,
                Timestamp = item.Timestamp
            };
            //closed previous activity 
            var newActivitiesBuffer = state.ActivitiesBuffer;
            if (state.CurrentActivity != null
                && state.CurrentActivity.AppName != null)
            {
                ProcessActivityInfo newInfo = new ProcessActivityInfo
                {
                    AppName = state.CurrentActivity.AppName,
                    Start = state.CurrentActivity.Timestamp,
                    Stop = item.Timestamp
                };
                newActivitiesBuffer = state.ActivitiesBuffer.Add(newInfo);
            }

            var tempState = new AppActivityFactGeneratorState
            {
                CurrentPeriod = currentPeriod,
                CurrentActivity = item,
                CurrentIdleEvent = nextIdleEvent,
                ActivitiesBuffer = newActivitiesBuffer,
                IdleActivitiesBuffer = state.IdleActivitiesBuffer
            };

            DateTime tempPointer = state.CurrentPeriod;
            List<AppActivityFact> newFacts = new List<AppActivityFact>();

            while ((item.Timestamp - tempPointer).TotalMinutes > periodMinutes)
            {
                var newCutOff = tempPointer.AddMinutes(periodMinutes);
                var result = Emit(tempState, newCutOff, periodMinutes);

                tempState = result.Item1;
                newFacts.AddRange(result.Item2);
                tempPointer = newCutOff;
            }
            return Tuple.Create(tempState, newFacts.ToImmutableArray());
        }

        /// handle idle
        static Tuple<AppActivityFactGeneratorState, ImmutableArray<AppActivityFact>> CalculateNextState(AppActivityFactGeneratorState state, IdleEvent idleItem)
        {
            if (idleItem == null)
            {
                throw new Exception();
            }

            if (state.CurrentIdleEvent == null)
            {
                return Tuple.Create(new AppActivityFactGeneratorState
                {
                    ActivitiesBuffer = state.ActivitiesBuffer,
                    IdleActivitiesBuffer = state.IdleActivitiesBuffer,
                    CurrentActivity = state.CurrentActivity,
                    CurrentPeriod = state.CurrentPeriod,
                    CurrentIdleEvent = idleItem
                }, ImmutableArray.Create<AppActivityFact>());
            }

            if (idleItem.Timestamp < state.CurrentPeriod)
            {
                log.WarnFormat("Idle event timestamp: {0} < current period: {1}", idleItem.Timestamp, state.CurrentPeriod);
                return Tuple.Create(state, ImmutableArray.Create<AppActivityFact>());
            }

            //add if it goes from idle to active
            var idleBuffer = state.IdleActivitiesBuffer;
            if (state.CurrentIdleEvent.State == IdleStateEnum.Idle
                && idleItem.State == IdleStateEnum.Active)
            {
                var newIdleInfo = new IdleActivityInfo
                {
                    Type = state.CurrentIdleEvent.Type,
                    Start = state.CurrentIdleEvent.Timestamp,
                    Stop = idleItem.Timestamp
                };
                idleBuffer = idleBuffer.Add(newIdleInfo);
            }

            //update idleEvent if it state changes
            var idleEvent = state.CurrentIdleEvent.State != idleItem.State ? idleItem : state.CurrentIdleEvent;

            return Tuple.Create(new AppActivityFactGeneratorState
            {
                ActivitiesBuffer = state.ActivitiesBuffer,
                IdleActivitiesBuffer = idleBuffer,
                CurrentActivity = state.CurrentActivity,
                CurrentPeriod = state.CurrentPeriod,
                CurrentIdleEvent = idleEvent
            }, ImmutableArray.Create<AppActivityFact>());
        }

        public static Tuple<AppActivityFactGeneratorState, ImmutableArray<AppActivityFact>> CalculateNextState(DateTime start, ImmutableArray<ProcessActivityInfo> activities, ImmutableArray<IdleActivityInfo> idleActivities, int periodMinutes)
        {
            AppActivityFactGeneratorState tempState = AppActivityFactGeneratorState.Create(start);

            //subtract all idleActivity before periodStart
            var subtractRange = new DateRange
            {
                Start = DateTime.MinValue,
                Stop = tempState.CurrentPeriod
            };
            var adjustedIdleActivities = idleActivities.Subtract(subtractRange);
            var eventItems = ConvertToEvents(activities, adjustedIdleActivities);

            List<AppActivityFact> emittedFacts = new List<AppActivityFact>();

            for (int i = 0; i < eventItems.Length; i++)
            {
                var item = eventItems[i];
                var result = CalculateNextState(tempState, item, periodMinutes);
                tempState = result.Item1;
                emittedFacts.AddRange(result.Item2);
            }
            return Tuple.Create(tempState, emittedFacts.ToImmutableArray());
        }
        //pre: appActiities are already segmented into the correct range
        public static AppActivityFact[] CalculateAppActivityFacts(ImmutableArray<ProcessActivityInfo> appActivities, ImmutableArray<IdleActivityInfo> idleActivities, int periodMinutes)
        {
            List<ExtendedAppActivityInfo> extendedAppActivities = ConvertToExtendedAppActivityInfo(appActivities, idleActivities);

            //check that there are no overlaps
            var hourLevelFacts = (from item in extendedAppActivities
                                  group item by new { item.AppName, PeriodStart = item.Start.ToPeriodStart(periodMinutes) } into g
                                  select new AppActivityFact
                                  {
                                      AppName = g.Key.AppName,
                                      DateTime = g.Key.PeriodStart,
                                      TotalDuration = g.ToList().Sum(item => item.GetDuration()),
                                      TotalIdleDuration = g.ToList().Sum(item => item.IdleDuration),
                                      TotalItems = g.ToList().Count
                                  }).ToList();

            //fill in the active durations
            hourLevelFacts.ForEach(item =>
            {
                item.TotalActiveDuration = item.TotalDuration - item.TotalIdleDuration;
            }
            );

            return hourLevelFacts.ToArray();
        }

        public static AppActivityFact[] CalculateAppActivityFacts2(DateTime periodStart, ImmutableArray<ProcessActivityInfo> appActivities, ImmutableArray<IdleActivityInfo> idleActivities)
        {
            List<ExtendedAppActivityInfo> extendedAppActivities = ConvertToExtendedAppActivityInfo(appActivities, idleActivities);

            //check that there are no overlaps
            var activityFacts = (from item in extendedAppActivities
                                 group item by item.AppName into g
                                 select new AppActivityFact
                                 {
                                     AppName = g.Key,
                                     DateTime = periodStart,
                                     TotalDuration = g.ToList().Sum(item => item.GetDuration()),
                                     TotalIdleDuration = g.ToList().Sum(item => item.IdleDuration),
                                     TotalItems = g.ToList().Count
                                 }).ToList();

            //fill in the active durations
            activityFacts.ForEach(item =>
            {
                item.TotalActiveDuration = item.TotalDuration - item.TotalIdleDuration;
            });

            return activityFacts.ToArray();
        }

        static ImmutableArray<AppActivityFactGeneratorEvent> ConvertToEvents(ImmutableArray<ProcessActivityInfo> appActivities, ImmutableArray<IdleActivityInfo> idleActivities)
        {
            var eventItems = ConvertToEvents2(appActivities).Concat(ConvertToEvents(idleActivities));

            var sorted = (from item in eventItems
                          orderby item.GetTimestamp() ascending
                          select item).ToImmutableArray();

            return sorted;
        }

        static ImmutableArray<AppActivityFactGeneratorEvent> ConvertToEvents2(ImmutableArray<ProcessActivityInfo> appActivities)
        {
            var eventStartItems = (from item in appActivities
                                   select new AppActivityFactGeneratorEvent
                                   {
                                       AppActivityEvent = new AppActivityEvent
                                       {
                                           Timestamp = item.Start,
                                           AppName = item.AppName
                                       }
                                   }).ToList();

            //calculate shutdowns
            var ranges = appActivities.Select(item => new DateRange
            {
                Start = item.Start,
                Stop = item.Stop
            }).ToImmutableArray();

            var mergedRanges = ranges.Merge();

            //exclude a range if it is active (DateTime.MaxValue)
            var eventStopItems = (from item in mergedRanges
                                  where item.Stop != DateTime.MaxValue
                                  select new AppActivityFactGeneratorEvent
                                  {
                                      AppActivityEvent = new AppActivityEvent
                                      {
                                          Timestamp = item.Stop,
                                          AppName = null
                                      }
                                  }).ToList();


            var combined = (from item in eventStartItems.Concat(eventStopItems)
                            orderby item.GetTimestamp() ascending
                            select item).ToImmutableArray();

            return combined;
        }

        static ImmutableArray<AppActivityFactGeneratorEvent> ConvertToEvents(ImmutableArray<IdleActivityInfo> idleActivities)
        {
            //merge the ranges since they may be overlapping
            var ranges = idleActivities.Select(item => new DateRange
            {
                Start = item.Start,
                Stop = item.Stop
            }).ToImmutableArray();

            var mergedRange = ranges.Merge();

            List<IdleEvent> list = new List<IdleEvent>();
            mergedRange.ToList().ForEach(item =>
            {
                list.Add(new IdleEvent
                {
                    State = IdleStateEnum.Idle,
                    Timestamp = item.Start
                });

                list.Add(new IdleEvent
                {
                    State = IdleStateEnum.Active,
                    Timestamp = item.Stop
                });
            });

            var sorted = (from item in list
                          orderby item.Timestamp ascending
                          select item).ToList();

            var converted = sorted.Select(item => new AppActivityFactGeneratorEvent
            {
                IdleEvent = item
            });
            return converted.ToImmutableArray();
        }

        static Tuple<AppActivityFactGeneratorState, ImmutableArray<AppActivityFact>> Emit(AppActivityFactGeneratorState currentState, DateTime cutOff, int periodMinutes)
        {
            if (cutOff <= currentState.CurrentPeriod)
            {
                throw new Exception("New cutoff lower than current hour");
            }

            var emitRange = new DateRange
            {
                Start = currentState.CurrentPeriod,
                Stop = cutOff
            };

            var relevantActivities = currentState.ActivitiesBuffer.Intersect(emitRange);
            var relevantIdleActivities = currentState.IdleActivitiesBuffer.Intersect(emitRange);

            //if currently idle, create the idleActivityInfo to the cutOff
            if (currentState.CurrentIdleEvent.State == IdleStateEnum.Idle)
            {
                relevantIdleActivities = relevantIdleActivities.Add(new IdleActivityInfo
                {
                    Start = currentState.CurrentIdleEvent.Timestamp,
                    Stop = cutOff
                });
            }
            var emission = ImmutableArray.Create<AppActivityFact>();
            if (relevantActivities.Length > 0)
            {
                emission = CalculateAppActivityFacts2(currentState.CurrentPeriod, relevantActivities, relevantIdleActivities).ToImmutableArray();
            }

            //trim off buffer up to cutOff 
            var newActivityBuffer = currentState.ActivitiesBuffer.Subtract(new DateRange { Start = DateTime.MinValue, Stop = cutOff });
            var newIdleActivityBuffer = currentState.IdleActivitiesBuffer.Subtract(new DateRange { Start = DateTime.MinValue, Stop = cutOff });

            return Tuple.Create(new AppActivityFactGeneratorState
            {
                CurrentPeriod = cutOff,
                CurrentActivity = currentState.CurrentActivity,
                CurrentIdleEvent = currentState.CurrentIdleEvent,
                IdleActivitiesBuffer = newIdleActivityBuffer,
                ActivitiesBuffer = newActivityBuffer
            }, emission);
        }

        public static List<ExtendedAppActivityInfo> ConvertToExtendedAppActivityInfo(ImmutableArray<ProcessActivityInfo> appActivityList, ImmutableArray<IdleActivityInfo> idleList)
        {
            List<ExtendedAppActivityInfo> normalizedList = new List<ExtendedAppActivityInfo>();

            List<DateRange> idleDateRanges = idleList.ToList().ConvertAll(item => new DateRange { Start = item.Start, Stop = item.Stop });

            string[] videoAppNames = new string[] { "www.youtube.com" };

            foreach (var item in appActivityList)
            {
                double idleDuration = 0;

                if (!videoAppNames.Contains(item.AppName))
                {
                    var activeOnly = item.Subtract(idleDateRanges);
                    double activeDuration = activeOnly.ToList().Sum(i => i.GetDuration());
                    idleDuration = item.GetDuration() - activeDuration;
                }

                ExtendedAppActivityInfo temp = new ExtendedAppActivityInfo
                {
                    AppName = item.AppName,
                    Start = item.Start,
                    Stop = item.Stop,
                    IdleDuration = idleDuration
                };

                normalizedList.Add(temp);
            }

            return normalizedList;
        }
    }
}
