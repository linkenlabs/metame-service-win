using MetaMe.Core;
using MetaMe.Sensors;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace MetaMe.WindowsClient
{
    class AppActivityFactGeneratorState
    {
        public DateTime CurrentPeriod { get; set; }
        public AppActivityEvent CurrentActivity { get; set; }
        public IdleEvent CurrentIdleEvent { get; set; } //for partial only
        public ImmutableArray<ProcessActivityInfo> ActivitiesBuffer { get; set; }
        public ImmutableArray<IdleActivityInfo> IdleActivitiesBuffer { get; set; }

        public static AppActivityFactGeneratorState Create(DateTime pointer)
        {
            return new AppActivityFactGeneratorState
            {
                CurrentPeriod = pointer,
                CurrentActivity = null,
                CurrentIdleEvent = null,
                ActivitiesBuffer = ImmutableArray.Create<ProcessActivityInfo>(),
                IdleActivitiesBuffer = ImmutableArray.Create<IdleActivityInfo>()
            };
        }
        public static AppActivityFactGeneratorState Create(DateTime pointer, ImmutableArray<ProcessActivityInfo> activities, int periodMins)
        {
            DateTime periodStart = DateTime.MinValue;

            if (activities.Length > 0)
            {
                //use minimum value
                var minDate = (from a in activities
                               select a.Start).Min();

                periodStart = minDate.ToPeriodStart(periodMins);
            }

            if (pointer > periodStart)
            {
                periodStart = pointer;
            }

            return new AppActivityFactGeneratorState
            {
                CurrentPeriod = periodStart,
                CurrentActivity = null,
                CurrentIdleEvent = null,
                ActivitiesBuffer = ImmutableArray.Create<ProcessActivityInfo>(),
                IdleActivitiesBuffer = ImmutableArray.Create<IdleActivityInfo>()
            };
        }
    }
}
