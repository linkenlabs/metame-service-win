using log4net;
using MetaMe.Core;
using MetaMe.Sensors;
using Microsoft.Ccr.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace MetaMe.WindowsClient
{
    class AppActivityFactGenerator
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public event EventHandler<AppActivityFactGeneratorStateChangedEventArgs> StateChanged;

        AppActivityFactGeneratorState _state;
        readonly Port<AppActivityFactGeneratorInitializeRequest> _initializationPort = new Port<AppActivityFactGeneratorInitializeRequest>();
        readonly Port<AppActivityFactGeneratorEvent> _ingestionPort = new Port<AppActivityFactGeneratorEvent>();
        readonly Port<ResetRequest> _resetPort = new Port<ResetRequest>();
        readonly int _periodMinutes;

        public AppActivityFactGenerator(DispatcherQueue queue, int periodMins)
        {
            _periodMinutes = periodMins;
            Arbiter.Activate(queue,
                Arbiter.Interleave(
                    new TeardownReceiverGroup(),
                    new ExclusiveReceiverGroup(
                        Arbiter.Receive(true, _initializationPort, HandleInitialize),
                        Arbiter.Receive(true, _ingestionPort, HandleIngestion),
                    Arbiter.Receive(true, _resetPort, HandleReset)),
                    new ConcurrentReceiverGroup()));
        }

        //pre: repository should be hooked up to HourLevelFactsCreated event before calling.
        public System.Threading.Tasks.Task InitializeAsync(DateTime pointer)
        {
            log.Info("Initializing...");
            TaskCompletionSource<bool> deferred = new TaskCompletionSource<bool>();

            AppActivityFactGeneratorInitializeRequest request = new AppActivityFactGeneratorInitializeRequest
            {
                Pointer = pointer,
                TaskCompletionSource = deferred
            };

            _initializationPort.Post(request);

            return deferred.Task;
        }

        public System.Threading.Tasks.Task Reset()
        {
            TaskCompletionSource<bool> deferred = new TaskCompletionSource<bool>();
            _resetPort.Post(new ResetRequest
            {
                TaskCompletionSource = deferred
            });
            return deferred.Task;
        }

        void HandleReset(ResetRequest request)
        {
            _state = null;
            StateChanged = null;
            request.TaskCompletionSource.SetResult(true);
        }

        void HandleInitialize(AppActivityFactGeneratorInitializeRequest request)
        {
            try
            {
                ImmutableArray<ProcessActivityInfo> activities = ClientApplication.Instance.GetAppActivityInfo().SkipTill(request.Pointer);
                ImmutableArray<IdleActivityInfo> idleActivities = ClientApplication.Instance.GetIdleActivityInfo().SkipTill(request.Pointer);

                //Trim activities and idleActivities to hourStart
                var subtractRange = new DateRange
                {
                    Start = DateTime.MinValue,
                    Stop = request.Pointer
                };

                var adjustedActivities = activities.Subtract(subtractRange);
                var adjustedIdleActivities = idleActivities.Subtract(subtractRange);

                var periodStart = AppActivityFactUtils.GetPeriodStart(adjustedActivities, _periodMinutes);

                var result = AppActivityFactUtils.CalculateNextState(periodStart, adjustedActivities, adjustedIdleActivities, _periodMinutes);

                var state = result.Item1;
                if (state.CurrentPeriod == DateTime.MinValue)
                {
                    state.CurrentPeriod = DateTime.UtcNow.ToPeriodStart(_periodMinutes);
                }
                _state = state;
                StateChanged?.Invoke(this, new AppActivityFactGeneratorStateChangedEventArgs
                {
                    State = _state,
                    Facts = result.Item2
                });

                log.Info("Ready...");
                request.TaskCompletionSource.SetResult(true);
            }
            catch (Exception ex)
            {
                log.Error("HandleInitialize Error", ex);
                request.TaskCompletionSource.SetException(ex);
            }
        }

        void HandleIngestion(AppActivityFactGeneratorEvent item)
        {
            var result = AppActivityFactUtils.CalculateNextState(_state, item, _periodMinutes);
            _state = result.Item1;

            StateChanged?.Invoke(this, new AppActivityFactGeneratorStateChangedEventArgs
            {
                State = result.Item1,
                Facts = result.Item2
            });
        }

        public AppActivityFactGeneratorState GetGeneratorState()
        {
            return _state;
        }

        //assumption: idle activities only occur within an AppActivity. Eg.You can't idle across two different apps
        public void IngestAsync(IdleEvent idleEvent)
        {
            _ingestionPort.Post(new AppActivityFactGeneratorEvent
            {
                IdleEvent = idleEvent
            });
        }

        //pre: nextActivity.Start should be completedActivity.Stop. otherwise null
        public void IngestAsync(AppActivityEvent item)
        {
            _ingestionPort.Post(new AppActivityFactGeneratorEvent
            {
                AppActivityEvent = item
            });
        }

        public ImmutableArray<AppActivityFact> GetPartialHourLevelFacts(bool calculateDelta = true)
        {
            if (_state == null)
            {
                return ImmutableArray.Create<AppActivityFact>();
            }

            if (!calculateDelta)
            {
                return AppActivityFactUtils.CalculateAppActivityFacts(_state.ActivitiesBuffer, _state.IdleActivitiesBuffer, _periodMinutes).ToImmutableArray();
            }
            //delta case
            DateTime utcNow = DateTime.UtcNow;

            var tempActivities = _state.ActivitiesBuffer;

            if (_state.CurrentActivity != null)
            {
                tempActivities = tempActivities.Add(new ProcessActivityInfo
                {
                    AppName = _state.CurrentActivity.AppName,
                    Start = _state.CurrentActivity.Timestamp,
                    Stop = utcNow
                });
            }

            var tempIdleActivities = _state.IdleActivitiesBuffer;

            if (_state.CurrentIdleEvent != null
                && _state.CurrentIdleEvent.State == IdleStateEnum.Idle)
            {
                tempIdleActivities = tempIdleActivities.Add(new IdleActivityInfo
                {
                    Type = null,
                    Start = _state.CurrentIdleEvent.Timestamp,
                    Stop = DateTime.MaxValue
                });
            }

            //return everything in the buffer rolled up
            var facts = AppActivityFactUtils.CalculateAppActivityFacts(tempActivities, tempIdleActivities, _periodMinutes).ToImmutableArray();

            return facts;
        }

    }
}
