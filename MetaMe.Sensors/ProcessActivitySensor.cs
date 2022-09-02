using log4net;
using MetaMe.Core;
using Microsoft.Ccr.Core;
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MetaMe.Sensors
{
    //Extracts URL if browser
    class ProcessActivitySensor
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public event EventHandler<ProcessActivityDetectedEventArgs> ActivityDetected;

        ProcessActivityInfo _current;

        readonly ForegroundSensor _foregroundSensor;
        readonly Port<ForegroundChangedEventArgs> _messagePort = new Port<ForegroundChangedEventArgs>();
        ImmutableArray<IUrlExtractor> _urlExtractors;
        EventHandler<ForegroundChangedEventArgs> _startedHandler;

        public ProcessActivitySensor(DispatcherQueue queue)
        {
            _urlExtractors = ImmutableArray.Create<IUrlExtractor>(new ChromeUrlExtractor(), new FirefoxUrlExtractor(), new BraveUrlExtractor(), new EdgeUrlExtractor());

            _foregroundSensor = new ForegroundSensor(queue, _urlExtractors);
            _foregroundSensor.ForegroundChanged += (s, e) =>
            {
                _messagePort.Post(e);
            };

            Arbiter.Activate(queue,
                Arbiter.Interleave(
                    new TeardownReceiverGroup(),
                    new ExclusiveReceiverGroup(
                        Arbiter.Receive(true, _messagePort, HandleSensorMessage)),
                    new ConcurrentReceiverGroup()));
        }

        public void Reset()
        {
            log.Info("Resetting...");
            _foregroundSensor.Started -= _startedHandler;
            _foregroundSensor.Stop();
            //close off current app activity

            ProcessActivityInfo current = new ProcessActivityInfo
            {
                ProcessLocation = _current.ProcessLocation,
                ProcessName = _current.ProcessName,
                Url = _current.Url,
                AppName = _current.AppName,
                WindowText = _current.WindowText,
                Start = _current.Start,
                Stop = DateTime.UtcNow
            };

            ActivityDetected?.Invoke(this, new ProcessActivityDetectedEventArgs
            {
                Previous = current,
                Current = null
            });
            _current = null;
            ActivityDetected = null;
        }

        public System.Threading.Tasks.Task Start()
        {
            TaskCompletionSource<bool> deferred = new TaskCompletionSource<bool>();
            try
            {
                log.Info("Starting...");

                _startedHandler = (s, e) =>
                {
                    HandleSensorMessage(e);
                    deferred.SetResult(true);
                    log.Info("Ready...");
                };

                _foregroundSensor.Started += _startedHandler;

                _foregroundSensor.Start();
            }
            catch (Exception ex)
            {
                log.Error("Start Error", ex);
                deferred.SetException(ex);
            }
            return deferred.Task;
        }

        void HandleSensorMessage(ForegroundChangedEventArgs message)
        {
            //handle back in time
            //we don't want negative time intervals
            if (_current != null
                && message.DateTime < _current.Start)
            {
                log.Warn("Negative time flow detected. Discarding...");
                log.Warn(new
                {
                    message.ProcessInfo.Name,
                    message.DateTime
                });
                return;
            }

            try
            {
                log.DebugFormat("Handling sensor message: {0}", message.ProcessInfo.Name);

                string windowText = WindowsApiUtils.GetWindowText(message.WindowHandle);
                IUrlExtractor urlExtractor = (from e in _urlExtractors
                                              where e.CanExtract(message.WindowHandle, message.ProcessInfo.Name, message.ClassName, windowText)
                                              select e).FirstOrDefault();

                string url = string.Empty;
                if (urlExtractor != null)
                {
                    url = urlExtractor.ExtractUrl(message.WindowHandle);
                    log.DebugFormat("Url extracted: {0}", url);
                }

                //handle duplicates events 
                if (_current != null
                    && message.DateTime != _current.Start
                    && _current.ProcessName == message.ProcessInfo.Name
                    && _current.WindowText == windowText
                    && _current.Url == url)
                {
                    log.DebugFormat("App repeated consecutively. Discarding...");
                    return;
                }

                //previousActivity is _current with updated stop
                ProcessActivityInfo previousActivity = _current == null ? null : new ProcessActivityInfo
                {
                    AppName = _current.AppName,
                    ProcessLocation = _current.ProcessLocation,
                    ProcessName = _current.ProcessName,
                    Url = _current.Url,
                    WindowText = _current.WindowText,
                    Start = _current.Start,
                    Stop = message.DateTime
                };

                string appName = GetAppName2(urlExtractor != null, url, message.ProcessInfo.Name, windowText, message.ProcessInfo.Path);

                ProcessActivityInfo currentActivity = new ProcessActivityInfo
                {
                    AppName = appName,
                    ProcessLocation = message.ProcessInfo.Path,
                    ProcessName = message.ProcessInfo.Name,
                    Url = url,
                    WindowText = windowText,
                    Start = message.DateTime,
                    Stop = DateTime.MaxValue
                };

                log.DebugFormat("Emitting ActivityDetected: {0} - {1}", appName, message.ProcessInfo.Name);

                ActivityDetected?.Invoke(this, new ProcessActivityDetectedEventArgs
                {
                    Previous = previousActivity,
                    Current = currentActivity
                });

                _current = currentActivity;
            }
            catch (Exception ex)
            {
                log.Warn(ex);
            }
        }

        public AppActivityEvent GetCurrentActivity()
        {
            if (_current == null)
            {
                return null;
            }

            return new AppActivityEvent
            {
                AppName = _current.AppName,
                Timestamp = _current.Start
            };
        }

        private string GetAppName2(bool isBrowser, string url, string processName, string windowText, string processPath)
        {
            if (processName == UWP.ProcessName
                && !string.IsNullOrEmpty(windowText)) //UWP Apps names are typically in the window title
            {
                return windowText;
            }

            string appName = processName;

            if (!string.IsNullOrEmpty(processPath) && File.Exists(processPath))
            {
                FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(processPath);
                appName = !string.IsNullOrEmpty(versionInfo.FileDescription) ? versionInfo.FileDescription : processName;
            }

            if (isBrowser)
            {
                if (!string.IsNullOrEmpty(url))
                {
                    string urlDomain = UrlUtils.ExtractUrlHost(url);

                    if (string.IsNullOrEmpty(urlDomain))
                    {
                        return string.Format("{0} - Other", appName);
                    }
                    else
                    {
                        return urlDomain;
                    }
                }
                else
                {
                    return string.Format("{0} - Other", appName);
                }
            }
            else
            {
                if (processName == Chrome.ProcessName
                    && !string.IsNullOrEmpty(windowText)) //Handle chrome apps
                {
                    return windowText;
                }
                return appName;
            }
        }
    }
}
