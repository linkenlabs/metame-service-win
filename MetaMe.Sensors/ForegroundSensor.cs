using log4net;
using Microsoft.Ccr.Core;
using System;
using System.Collections.Immutable;
using UIAutomationBlockingCoreLib;

namespace MetaMe.Sensors
{
    //Have minimal processing at this layer
    class ForegroundSensor
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public event EventHandler<ForegroundChangedEventArgs> ForegroundChanged;
        public event EventHandler<ForegroundChangedEventArgs> Started;
        public event EventHandler Stopped;

        private const uint WINEVENT_OUTOFCONTEXT = 0;
        private const uint EVENT_OBJECT_NAMECHANGE = 0x800C;
        private const uint EVENT_SYSTEM_FOREGROUND = 3;
        private const uint OBJID_WINDOW = 0x00000000;
        ImmutableArray<IUrlExtractor> _urlExtractors;
        readonly Port<WindowEventHookMessage> _messagePort = new Port<WindowEventHookMessage>();
        bool _started = false;
        readonly WindowEventHookSensor _winHookSensor;

        public ForegroundSensor(DispatcherQueue queue, ImmutableArray<IUrlExtractor> urlExtractors)
        {
            _urlExtractors = urlExtractors;

            Arbiter.Activate(queue,
                Arbiter.Interleave(
                    new TeardownReceiverGroup(),
                    new ExclusiveReceiverGroup(
                        Arbiter.Receive(true, _messagePort, HandleWindowEventHookMessage)),
                    new ConcurrentReceiverGroup()));

            _winHookSensor = new WindowEventHookSensor(queue);
            _winHookSensor.MessageDetected += (sender, e) =>
            {
                _messagePort.Post(e);
            };
        }

        public void Start()
        {
            log.Info("Started...");
            //get foreground window
            var eventTime = DateTime.UtcNow;
            var handle = NativeMethods.GetForegroundWindow();
            string className = WindowsApiUtils.GetClassNameOfWindow(handle);
            var processInfo = ProcessInfo.GetProcessInfo(handle);

            _started = true;
            Started?.Invoke(this, new ForegroundChangedEventArgs
            {
                ProcessInfo = processInfo,
                DateTime = eventTime,
                ClassName = className,
                WindowHandle = handle
            });
        }

        public void Stop()
        {
            log.Info("Stopping...");
            _started = false;
            Stopped?.Invoke(this, new EventArgs());
        }

        void HandleWindowEventHookMessage(WindowEventHookMessage message)
        {
            try
            {
                if (!IsValidEvent(message.WindowHandle, message.IdObject))
                {
                    return;
                }

                var foregroundHandle = NativeMethods.GetForegroundWindow();
                if (foregroundHandle != message.WindowHandle)
                {
                    return;
                }

                string className = WindowsApiUtils.GetClassNameOfWindow(message.WindowHandle);

                if (!IsValidClassName(className))
                {
                    return;
                }

                ProcessInfo processInfo = ProcessInfo.GetProcessInfo(message.WindowHandle); //todo: optimize. taking alot of CPU
                if (processInfo == null)
                {
                    return;
                }

                if (message.Type == WindowEventHookMessageType.EventObjectNameChange
                    && IsToolTip(message.WindowHandle))
                {
                    return;
                }
                log.DebugFormat("Posting to _messagePort: [EventSystemForegroundChange] {0}", processInfo.Name);

                DateTime utcTime = DateTime.UtcNow;
                ForegroundChanged?.Invoke(this, new ForegroundChangedEventArgs
                {
                    DateTime = utcTime,
                    ProcessInfo = processInfo,
                    ClassName = className,
                    WindowHandle = message.WindowHandle
                });
            }
            catch (Exception ex)
            {
                log.Warn(ex);
            }
        }

        static bool IsToolTip(IntPtr windowHandle)
        {
            try
            {
                CUIAutomation automation = new CUIAutomation();

                IUIAutomationCacheRequest valueCacheRequest = automation.CreateCacheRequest();
                valueCacheRequest.AddProperty(UIAProperties.UIA_ControlTypePropertyId);
                valueCacheRequest.AutomationElementMode = AutomationElementMode.AutomationElementMode_None;

                IUIAutomationElement element = automation.ElementFromHandleBuildCache(windowHandle, valueCacheRequest);

                return element.CachedControlType == UIAControlTypes.UIA_ToolTipControlTypeId;
            }
            catch (Exception ex)
            {
                log.Warn(ex);
                return false;
            }
        }

        bool IsValidEvent(IntPtr hwnd, int idObject)
        {
            if (!_started)
            {
                return false;
            }

            if (idObject != OBJID_WINDOW)
            {
                return false;
            }

            if (hwnd == null
                || hwnd == IntPtr.Zero)
            {
                return false;
            }

            return true;
        }

        static bool IsValidClassName(string className)
        {
            if (className == "DirectUIHWND"
                || className == "Windows.UI.Core.CoreWindow")
            {
                return false;
            }

            return true;
        }
    }
}
