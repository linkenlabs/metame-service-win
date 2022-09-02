using log4net;
using MetaMe.Core;
using MetaMe.Sensors;
using MetaMe.WindowsClient.controllers;
using Microsoft.Ccr.Core;
using System;
using System.Collections.Immutable;
using System.IO;
using System.Reactive.Subjects;
using System.Reflection;

namespace MetaMe.WindowsClient
{
    partial class ClientApplication
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private const int ROLLING_RECORD_LIMIT = 10000;

        public static ClientApplication Instance { get; } = new ClientApplication();

        readonly DispatcherQueue _dispatcherQueue;
        readonly DispatcherQueue _priorityQueue;

        readonly IdleSensor _idleSensor;
        readonly PowerModeSensor _powerModeSensor;
        readonly ProcessActivitySensor _appActivitySensor;
        readonly IconSaver _iconSaver;
        readonly NotificationGenerator _notificationGenerator;
        readonly JobProcessor _jobProcessor;

        //stream processors
        readonly AppActivityFactGenerator _hourLevelGenerator;
        readonly AppActivityFactGenerator _fifteenMinuteFactGenerator;

        readonly CandleGenerator _hourCandleGenerator; //for live dashboard
        readonly CandleGenerator _fifteenMinuteCandleGenerator;

        readonly ApplicationScanner _applicationScanner;
        readonly Port<Exception> _exceptionPort;

        public bool DevMode { get; private set; } = false;
        public ProcessActivityInfo ActiveForeground { get; set; }
        public ClientApplicationStatus Status { get; private set; } = ClientApplicationStatus.Initializing;
        public ClientApplicationAccountStatus AccountStatus { get; private set; } = ClientApplicationAccountStatus.LoggedIn;
        public Exception Exception { get; private set; }
        public InitializeProgressData InitializationProgress { get; private set; }

        private HiddenAppList _hiddenApp;
        private readonly Subject<HiddenAppList> _hiddenAppSource = new Subject<HiddenAppList>();

        private Setting _settings;
        private readonly Subject<Setting> _settingsSource = new Subject<Setting>();

        private ImmutableArray<Group> _groups;
        private readonly Subject<ImmutableArray<Group>> _groupsSource = new Subject<ImmutableArray<Group>>();

        private ImmutableArray<Goal> _goals;
        private readonly Subject<ImmutableArray<Goal>> _goalsSource = new Subject<ImmutableArray<Goal>>();

        private ImmutableArray<IdleActivityInfo> _idleActivities = ImmutableArray<IdleActivityInfo>.Empty;
        private ImmutableArray<ProcessActivityInfo> _appActivities = ImmutableArray<ProcessActivityInfo>.Empty;

        public IObservable<ImmutableArray<Goal>> GoalsSource
        {
            get
            {
                return _goalsSource;
            }
        }

        private ImmutableArray<AppActivityFact> _hourLevelFacts = ImmutableArray<AppActivityFact>.Empty;
        private ImmutableArray<AppActivityFact> _fifteenMinuteLevelFacts = ImmutableArray<AppActivityFact>.Empty;

        private readonly Subject<ImmutableArray<AppActivityFact>> _hourLevelFactSource = new Subject<ImmutableArray<AppActivityFact>>();
        private readonly Subject<ImmutableArray<AppActivityFact>> _fifteenMinuteLevelFactSource = new Subject<ImmutableArray<AppActivityFact>>();

        public ImmutableArray<NotificationInfo> Notifications = ImmutableArray<NotificationInfo>.Empty;

        private ClientApplication()
        {
            Dispatcher dispatcher = new Dispatcher(0, "MetaMe dispatcher");
            _dispatcherQueue = new DispatcherQueue("Normal queue", dispatcher);
            _priorityQueue = new DispatcherQueue("Priority queue", dispatcher);
            // create a causality using the port instance
            _exceptionPort = new Port<Exception>();
            Causality exampleCausality = new Causality("Root cause", _exceptionPort);

            // add causality to current thread
            Dispatcher.AddCausality(exampleCausality);

            Arbiter.Activate(_dispatcherQueue,
                Arbiter.Receive(true, _exceptionPort, HandleException));

            _hourLevelGenerator = new AppActivityFactGenerator(_priorityQueue, 60);
            _hourCandleGenerator = new CandleGenerator(60, 24 * 4);

            _fifteenMinuteFactGenerator = new AppActivityFactGenerator(_priorityQueue, 15);
            _fifteenMinuteCandleGenerator = new CandleGenerator(15, 24 * 4);

            _appActivitySensor = new ProcessActivitySensor(_priorityQueue);
            _idleSensor = new IdleSensor(_priorityQueue);
            _powerModeSensor = new PowerModeSensor();
            _notificationGenerator = new NotificationGenerator(_dispatcherQueue, this);
            _jobProcessor = new JobProcessor(_dispatcherQueue);

            _applicationScanner = new ApplicationScanner(_dispatcherQueue);
            _iconSaver = new IconSaver(_dispatcherQueue);
        }

        void HandleException(Exception ex)
        {
            log.Error(ex);
            Exception = ex;
        }

        public AppActivityEvent GetCurrentAppActivityEvent()
        {
            return _appActivitySensor.GetCurrentActivity();
        }

        public IdleEvent GetCurrentIdleEvent()
        {
            return _hourLevelGenerator.GetGeneratorState().CurrentIdleEvent;
        }

        public Guid Export(ExportCsvRequest request)
        {
            return _jobProcessor.Process(request);
        }

        public JobState GetJobState(Guid guid)
        {
            var jobInfo = _jobProcessor.GetJobProcessInfo(guid);
            return jobInfo?.State;
        }

        internal System.Threading.Tasks.Task<bool> Stop()
        {
            log.Info("Stopping...");
            return System.Threading.Tasks.Task.Run(async () =>
            {
                Status = ClientApplicationStatus.Stopping;

                //remove all queued till none left
                _dispatcherQueue.Suspend();
                _priorityQueue.Suspend();

                ITask task;
                while (_dispatcherQueue.TryDequeue(out task))
                {
                    log.Info("_dispatcherQueue: Task dequeued");
                }

                while (_priorityQueue.TryDequeue(out task))
                {
                    log.Info("_priorityQueue: Task dequeued");
                }

                //restart dispatcher queues
                _dispatcherQueue.Resume();
                _priorityQueue.Resume();

                //Yeap pretty hacky, give some time for resumed saves and tasks to complete.
                while (_dispatcherQueue.Dispatcher.PendingTaskCount > 0)
                {
                    System.Threading.Thread.Sleep(1000);
                }

                //pending task counts may be zero, but there may be some currently executing tasks that need to finish
                //kinda hacky but give 1 seconds for safety
                System.Threading.Thread.Sleep(1000);

                _appActivitySensor.Reset();
                _idleSensor.Reset();

                _hourCandleGenerator.Reset();

                await _hourLevelGenerator.Reset();
                _settingsSource.Dispose();
                _hiddenAppSource.Dispose();
                _goalsSource.Dispose();
                _groupsSource.Dispose();
                _hourLevelFactSource.Dispose();

                AccountStatus = ClientApplicationAccountStatus.LoggedOut;

                log.Info("Stopped");

                return true;
            });
        }

        public void Delete()
        {
            //Delete data
            string deviceFolder = PathUtils.GetApplicationDeviceFolder(DevMode);
            string[] files = Directory.GetFiles(deviceFolder);
            foreach (var item in files)
            {
                File.Delete(item);
            }

            Directory.Delete(deviceFolder);
            Passphrase.DeletePassphraseKeyFile(DevMode);
        }

        public TimeSpan GetUtcStartTimeOfDay()
        {
            var settingsInfo = GetSettings();

            //settingsInfo.StartTime
            int hours = Convert.ToInt32(settingsInfo.StartTime.Split(':')[0]);

            //find startTime in UTC
            DateTime localizedStartTime = DateTime.Now.Date.AddHours(hours);
            DateTime utcStartTime = localizedStartTime.ToUniversalTime();
            return utcStartTime.TimeOfDay;
        }

        public ImmutableArray<CandleSeries> GetHourCandleSeriesData()
        {
            return _hourCandleGenerator.GetCandleSeriesData();
        }
        public ImmutableArray<CandleSeries> GetFifteenMinuteCandleSeriesData()
        {
            return _fifteenMinuteCandleGenerator.GetCandleSeriesData();
        }

        public ImmutableArray<ProcessActivityInfo> GetAppActivityInfo()
        {
            return _appActivities;
        }

        public ImmutableArray<IdleActivityInfo> GetIdleActivityInfo()
        {
            return _idleActivities;
        }

        public ImmutableArray<Goal> GetGoals()
        {
            return _goals;
        }

        public void SetGoals(ImmutableArray<Goal> goals)
        {
            _goalsSource.OnNext(goals);
        }

        public ImmutableArray<Group> GetGroups()
        {
            return _groups;
        }

        public void SetGroups(ImmutableArray<Group> groups)
        {
            _groupsSource.OnNext(groups);
        }

        public ImmutableArray<ApplicationScanInfo> GetScannedApplications()
        {
            return _applicationScanner.GetScannedApplications();
        }

        public ImmutableArray<AppActivityFact> GetHourLevelFacts()
        {
            var historicalData = _hourLevelFacts;
            var partialData = _hourLevelGenerator.GetPartialHourLevelFacts();
            var mergedData = historicalData.AddRange(partialData);

            return mergedData;
        }

        public ImmutableArray<AppActivityFact> GetFifteenMinuteFacts()
        {
            var historicalData = _fifteenMinuteLevelFacts;
            var partialData = _fifteenMinuteFactGenerator.GetPartialHourLevelFacts();
            var mergedData = historicalData.AddRange(partialData);
            return mergedData;
        }

        //get only stuff which has stopped changing
        public ImmutableArray<AppActivityFact> GetHistoricalHourLevelFacts()
        {
            var historicalData = _hourLevelFacts;
            return historicalData;
        }

        public void SaveSettings(Setting settings)
        {
            _settingsSource.OnNext(settings);
        }

        public Setting GetSettings()
        {
            return _settings;
        }

        public HiddenAppList GetHiddenAppList()
        {
            return _hiddenApp;
        }

        public void SaveHiddenAppList(HiddenAppList list)
        {
            _hiddenAppSource.OnNext(list);
        }
    }
}
