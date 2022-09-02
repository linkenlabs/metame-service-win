using MetaMe.Core;
using MetaMe.Sensors;
using MetaMe.WindowsClient.Data;
using MetaMe.WindowsClient.Hubs;
using MetaMe.WindowsClient.Migrations;
using Microsoft.AspNet.SignalR;
using Newtonsoft.Json;
using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks.Dataflow;

namespace MetaMe.WindowsClient
{
    partial class ClientApplication
    {
        public async void Initialize(bool devMode)
        {
            var progress = new InitializeProgressData();
            InitializationProgress = progress;

            try
            {
                DevMode = devMode;
                Status = ClientApplicationStatus.Initializing;
                InitializeInternal(progress);
                await progress.TaskCompletionSource.Task;

                Status = ClientApplicationStatus.Started;
                log.Info("Started.");
            }
            catch (Exception ex)
            {
                Status = ClientApplicationStatus.Error;
                HandleException(ex);
            }
        }
        private async void InitializeInternal(InitializeProgressData progressData)
        {
            try
            {
                if (DatabaseInit.RequiresMigration())
                {
                    DatabaseInit.Migrate();
                }

                if (Migration202109.RequiresMigration())
                {
                    Migration202109.Migrate();
                }

                WireIconSaver();

                //INITIALIZE APPLICATION SCANNER
                progressData.Log("Initializing application scanner");
                _applicationScanner.Scan();

                //We want to perform the following initialize sequence.  This happens after the user has been Authenticated
                //Step 1. Initialize device Id, create if necessary
                //Step 2. Initialize settings
                //Step 3. Initialize slots
                //Step 4. Initialize AppActivity / IdleActivity state into HourLevelGenerator
                //Step 5. Regenerate HourLevelFacts if required
                //Step 6. Initialize Repositories
                //Step 7. Hook up other peripherals
                //Step 8. Start sensors

                //INITIALIZE SETTINGS
                progressData.Log("Loading settings");
                LoadSettings();

                //INITIALIZE HIDDEN APPS
                progressData.Log("Loading hidden apps list");
                LoadHiddenAppsList();

                progressData.Log("Loading groups");
                LoadGroups();

                progressData.Log("Loading goals");
                LoadGoals();

                //INITIALIZE REMAINING REPOSITORIES
                progressData.Log("Loading activity info");
                LoadActivityInfo();
                WireAppActivityInfoComponents();

                //INITIALIZE IDLEACTIVITYINFO
                progressData.Log("Loading idle info");
                LoadIdleActivity();
                WireIdleActivityInfoComponents();

                //Cleanup mneumonic and passphrase
                File.Delete(PathUtils.GetPassphraseKeyPath(DevMode));

                //INITIALIZE HOURLEVELGENERATOR
                progressData.Log("Loading hourLevelFacts");
                WireHourLevelFactSource();

                progressData.Log("Initializing hourLevelGenerator");
                WireFactGenerator(_hourLevelGenerator);
                //only start from previous point onwards
                DateTime pointer = _hourLevelFacts.GetPointer();
                await _hourLevelGenerator.InitializeAsync(pointer);

                //INITIALIZE OTHER PERIPHERALS
                WireActiveForeground();
                WireGroupsHub();
                WireGroupSeriesHub();

                //Setup candleGenerator
                progressData.Log("Initializing candle generator");
                WireCandleGenerator(_hourCandleGenerator, _hourLevelGenerator);

                _hourCandleGenerator.CandlesChanged += (s, e) =>
                {
                    if (Status == ClientApplicationStatus.Initializing)
                    {
                        progressData.Log("Initialization complete.");
                        progressData.TaskCompletionSource.SetResult(true);
                    }
                };

                //INITIALIZE HOURLEVELGENERATOR
                progressData.Log("Loading 15MinuteFacts");
                WireFifteenMinuteFactSource();

                //Create fifteenMinuteFactGenerator and connect inputs
                progressData.Log("Initializing 15MinuteFactGenerator...");
                WireFactGenerator(_fifteenMinuteFactGenerator);
                var startOfDay = DateTime.UtcNow.ToDayStart(GetUtcStartTimeOfDay());
                //get day start
                await _fifteenMinuteFactGenerator.InitializeAsync(startOfDay);

                //Create 15min candlegenerator
                progressData.Log("Initializing 15MinuteCandleGenerator...");
                WireCandleGenerator(_fifteenMinuteCandleGenerator, _fifteenMinuteFactGenerator);

                //FifteenMinuteGroupSeriesHub
                WireFifteenGroupSeriesHub();

                //START SENSORS
                progressData.Log("Initializing app sensors");
                await _appActivitySensor.Start();

                progressData.Log("Initializing idle sensors");
                _idleSensor.Start();

            }
            catch (Exception ex)
            {
                progressData.Exception = ex;
                progressData.TaskCompletionSource.SetException(ex);
            }
        }

        private void WireActiveForeground()
        {
            _appActivitySensor.ActivityDetected += (s, e) =>
            {
                ActiveForeground = e.Current;

            };
        }

        private void WireFifteenGroupSeriesHub()
        {
            _fifteenMinuteCandleGenerator.CandlesChanged += (s, e) =>
            {
                IHubContext hubContext = GlobalHost.ConnectionManager.GetHubContext<FifteenMinuteGroupSeriesHub>();
                hubContext.Clients.All.dataChanged(e.CandleSeries);
            };
        }

        private void WireGroupSeriesHub()
        {
            _hourCandleGenerator.CandlesChanged += (s, e) =>
            {
                IHubContext groupsSeriesHubContext = GlobalHost.ConnectionManager.GetHubContext<GroupSeriesHub>();
                groupsSeriesHubContext.Clients.All.dataChanged(e.CandleSeries);
                log.Debug("Hour candles emitted...");
            };
        }

        private void LoadIdleActivity()
        {
            string databasePath = PathUtils.GetSQLiteDatabasePath(DevMode);
            string connectionString = SQLiteUtils.CreateConnectionString(databasePath);

            var idleItems = DatabaseUtils.GetIdleActivities(connectionString);

            _idleActivities = idleItems.ToList().ConvertAll((datum) =>
            {
                return new IdleActivityInfo
                {
                    Type = datum.Type,
                    Start = DateTime.Parse(datum.Start, null, System.Globalization.DateTimeStyles.RoundtripKind),
                    Stop = DateTime.Parse(datum.Stop, null, System.Globalization.DateTimeStyles.RoundtripKind)
                };
            }).ToImmutableArray();
        }

        private void WireIdleActivityInfoComponents()
        {

            ActionBlock<IdleActivityInfo> writeBlock = new ActionBlock<IdleActivityInfo>((item) =>
            {
                string databasePath = PathUtils.GetSQLiteDatabasePath(DevMode);
                string connectionString = SQLiteUtils.CreateConnectionString(databasePath);

                var newRecord = new IdleActivity
                {
                    Type = item.Type,
                    Start = item.Start.ToString("o"),
                    Stop = item.Stop.ToString("o")
                };
                //add record
                DatabaseUtils.BulkInsertIdleActivity(ImmutableArray.Create(newRecord), connectionString);

            }, new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = 1,
                BoundedCapacity = 1
            });

            EventHandler<IdleStateChanged> handleStateChanged = (s, e) =>
            {
                if (e.IdleActivityInfo == null)
                {
                    return;
                }

                _idleActivities = _idleActivities.Add(e.IdleActivityInfo);
                writeBlock.Post(e.IdleActivityInfo);
            };

            _idleSensor.IdleStateChanged += handleStateChanged;
            _powerModeSensor.IdleStateChanged += handleStateChanged;
        }


        private void LoadActivityInfo()
        {
            string databasePath = PathUtils.GetSQLiteDatabasePath(DevMode);

            string connectionString = SQLiteUtils.CreateConnectionString(databasePath);

            //select all then set
            var activityRows = DatabaseUtils.GetAppActivities(connectionString);
            var appLookup = DatabaseUtils.GetApps(connectionString).ToDictionary(d => d.Id);

            //now convert to AppActivityInfo
            var appActivityInfoItems = activityRows.ToList().ConvertAll((activityRow) =>
            {

                return new ProcessActivityInfo
                {
                    AppName = appLookup[activityRow.AppId].Name,
                    Start = DateTime.Parse(activityRow.Start, null, System.Globalization.DateTimeStyles.RoundtripKind),
                    Stop = DateTime.Parse(activityRow.Stop, null, System.Globalization.DateTimeStyles.RoundtripKind)
                };
            });

            _appActivities = appActivityInfoItems.ToImmutableArray();
        }

        private void WireAppActivityInfoComponents()
        {
            string connectionString = SQLiteUtils.CreateConnectionString(PathUtils.GetSQLiteDatabasePath(DevMode));
            //used for discarding older tasks
            ActionBlock<ProcessActivityInfo> writeBlock = new ActionBlock<ProcessActivityInfo>((item) =>
            {
                //check for app record
                var appRecord = DatabaseUtils.GetAppByName(item.AppName, connectionString);

                if (appRecord == null)
                {
                    //insert
                    var newAppRecord = new App
                    {
                        Name = item.AppName,
                        IsWebsite = ApplicationInfoUtils.GetTypeFromAppName(item.AppName) == "website" ? 1 : 0
                    };
                    DatabaseUtils.BulkInsertAppRows(ImmutableArray.Create(newAppRecord), connectionString);
                    //check again
                    appRecord = DatabaseUtils.GetAppByName(item.AppName, connectionString);
                }
                var newRecord = new AppActivity
                {
                    AppId = appRecord.Id,
                    Start = item.Start.ToString("o"),
                    Stop = item.Stop.ToString("o")
                };
                //add record
                DatabaseUtils.BulkInsertAppActivityRow(ImmutableArray.Create(newRecord), connectionString);

            }, new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = 1,
                BoundedCapacity = 1
            });

            _appActivitySensor.ActivityDetected += (s, e) =>
            {
                //only store completed activities
                if (e.Previous == null)
                {
                    return;
                }
                //TODO: write to db new format
                _appActivities = _appActivities.Add(e.Previous);
                writeBlock.Post(e.Previous);
            };

        }

        private void WireHourLevelFactSource()
        {
            _hourLevelFactSource.ObserveOn(TaskPoolScheduler.Default).Subscribe(item =>
            {
                _hourLevelFacts = item;
            }, HandleException);

            //must be async otherwise buffer may not be ready by the time the rest of the app starts up
            _hourLevelGenerator.StateChanged += (s, e) =>
            {
                if (e.Facts.Length == 0)
                {
                    return;
                }
                _hourLevelFactSource.OnNext(_hourLevelFacts.AddRange(e.Facts));
            };
        }

        private void WireFifteenMinuteFactSource()
        {
            _fifteenMinuteLevelFactSource.ObserveOn(TaskPoolScheduler.Default).Subscribe(item =>
            {
                _fifteenMinuteLevelFacts = item;
            }, HandleException);

            //must be async otherwise buffer may not be ready by the time the rest of the app starts up
            _fifteenMinuteFactGenerator.StateChanged += (s, e) =>
            {
                if (e.Facts.Length == 0)
                {
                    return;
                }
                _fifteenMinuteLevelFactSource.OnNext(_fifteenMinuteLevelFacts.AddRange(e.Facts));
            };
        }

        private void LoadGoals()
        {
            var goalsPath = PathUtils.GetGoalsPath(DevMode);
            if (!File.Exists(goalsPath))
            {
                var defaultValue = ImmutableArray<Goal>.Empty;
                string jsonData = JsonConvert.SerializeObject(defaultValue);
                File.WriteAllText(goalsPath, jsonData);
            }

            var goalJson = File.ReadAllText(goalsPath);
            _goals = JsonConvert.DeserializeObject<ImmutableArray<Goal>>(goalJson);

            _goalsSource.ObserveOn(TaskPoolScheduler.Default).Subscribe(item =>
            {
                _goals = item;
                string jsonData = JsonConvert.SerializeObject(item);
                File.WriteAllText(goalsPath, jsonData);
            }, HandleException);
        }

        private void LoadGroups()
        {
            var groupsPath = PathUtils.GetGroupsPath(DevMode);
            if (!File.Exists(groupsPath))
            {
                //save default to file
                var defaultGroups = (new Group[]{
                    new Group { Name = "Productive", Items = Array.Empty<string>() },
                    new Group { Name = "Unproductive", Items = Array.Empty<string>() }
                   }).ToImmutableArray();

                var jsonData = JsonConvert.SerializeObject(defaultGroups);
                File.WriteAllText(groupsPath, jsonData);
            }

            //initialize
            var groupsJson = File.ReadAllText(groupsPath);
            _groups = JsonConvert.DeserializeObject<ImmutableArray<Group>>(groupsJson);

            //write to file on change
            _groupsSource.ObserveOn(TaskPoolScheduler.Default).Subscribe(item =>
            {
                _groups = item;
                var jsonData = JsonConvert.SerializeObject(item);
                File.WriteAllText(groupsPath, jsonData);

            }, HandleException);

        }

        private void WireGroupsHub()
        {
            _groupsSource.ObserveOn(TaskPoolScheduler.Default).Subscribe(item =>
            {
                IHubContext hubContext = GlobalHost.ConnectionManager.GetHubContext<GroupsHub>();
                hubContext.Clients.All.groupsChanged(item);
            }, HandleException);
        }

        private void LoadHiddenAppsList()
        {
            var hiddenAppsPath = PathUtils.GetHiddenAppsPath(DevMode);
            if (!File.Exists(hiddenAppsPath))
            {
                var defaultValue = new HiddenAppList
                {
                    Items = new string[] { "Idle", "LockApp.exe", "appvlp", "Search and Cortana application" }
                };

                var jsonData = JsonConvert.SerializeObject(defaultValue);
                File.WriteAllText(hiddenAppsPath, jsonData);
            }

            var hiddenAppsJson = File.ReadAllText(hiddenAppsPath);
            _hiddenApp = JsonConvert.DeserializeObject<HiddenAppList>(hiddenAppsJson);

            _hiddenAppSource.ObserveOn(TaskPoolScheduler.Default).Subscribe(item =>
            {
                _hiddenApp = item;
                var jsonData = JsonConvert.SerializeObject(item);
                File.WriteAllText(hiddenAppsPath, jsonData);
            }, HandleException);
        }

        private void LoadSettings()
        {
            var settingsPath = PathUtils.GetSettingsPath(DevMode);

            if (!File.Exists(settingsPath))
            {
                var defaultSettings = new Setting { StartDay = "Sunday", StartTime = "05:00" };
                var jsonData = JsonConvert.SerializeObject(defaultSettings);
                File.WriteAllText(settingsPath, jsonData);
            }

            var settingsJson = File.ReadAllText(settingsPath);
            _settings = JsonConvert.DeserializeObject<Setting>(settingsJson);

            _settingsSource.ObserveOn(TaskPoolScheduler.Default).Subscribe(item =>
            {
                _settings = item;
                var jsonData = JsonConvert.SerializeObject(item);
                File.WriteAllText(settingsPath, jsonData);
            }, HandleException);

        }

        private void WireIconSaver()
        {
            _appActivitySensor.ActivityDetected += (s, e) =>
            {
                _iconSaver.PostAsync(new IconSaverMessage
                {
                    ProcessPath = e.Current.ProcessLocation,
                    Url = e.Current.Url,
                    AppName = e.Current.AppName
                });
            };
        }

        void WireCandleGenerator(CandleGenerator generator, AppActivityFactGenerator factGenerator)
        {
            factGenerator.StateChanged += (s, e) =>
            {
                generator.UpdateCandles(e.Facts, e.State);
            };
            //CandleGenerator inputs
            _groupsSource.ObserveOn(TaskPoolScheduler.Default).Subscribe(item =>
            {
                generator.Refresh();
            });

            _hiddenAppSource.ObserveOn(TaskPoolScheduler.Default).Subscribe(item =>
            {
                generator.Refresh();
            });
        }

        void WireFactGenerator(AppActivityFactGenerator generator)
        {
            _appActivitySensor.ActivityDetected += (s, e) =>
            {
                //shutdown case
                if (e.Current == null)
                {
                    generator.IngestAsync(new AppActivityEvent
                    {
                        AppName = null,
                        Timestamp = e.Previous.Stop
                    });
                    return;
                }

                generator.IngestAsync(new AppActivityEvent
                {
                    AppName = e.Current.AppName,
                    Timestamp = e.Current.Start
                });
            };
            _idleSensor.IdleStateChanged += (s, e) =>
            {
                generator.IngestAsync(new IdleEvent
                {
                    State = e.State,
                    Timestamp = e.Timestamp,
                    Type = "Idle"
                });
            };
            _powerModeSensor.IdleStateChanged += (s, e) =>
            {
                generator.IngestAsync(new IdleEvent
                {
                    State = e.State,
                    Timestamp = e.Timestamp,
                    Type = "Suspended"
                });
            };
        }
    }
}
