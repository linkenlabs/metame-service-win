using log4net;
using Microsoft.Ccr.Core;
using System;

namespace MetaMe.Sensors
{
    public class WindowEventHookSensor
    {
        public event EventHandler<WindowEventHookMessage> MessageDetected;

        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private const uint WINEVENT_OUTOFCONTEXT = 0;
        private const uint EVENT_OBJECT_NAMECHANGE = 0x800C;
        private const uint EVENT_SYSTEM_FOREGROUND = 3;
        readonly NativeMethods.WinEventDelegate _handleForegroundChangeDelegate = null;
        readonly NativeMethods.WinEventDelegate _handleNameChangeDelegate = null;

        // serves as both an signal and a queue. 
        readonly Port<WindowEventHookMessage> _internalPort = new Port<WindowEventHookMessage>();

        private uint _lastEventTime;

        public WindowEventHookSensor(DispatcherQueue queue)
        {
            Arbiter.Activate(queue,
                Arbiter.Interleave(
                    new TeardownReceiverGroup(),
                    new ExclusiveReceiverGroup(
                        Arbiter.Receive(true, _internalPort, HandleMessage)),
                    new ConcurrentReceiverGroup()));

            //Initialization must happen on the elevated thread execution context for hooks to work
            _handleForegroundChangeDelegate = new NativeMethods.WinEventDelegate(HandleHookEventSystemForegroundChange);
            IntPtr hook1 = NativeMethods.SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND, IntPtr.Zero, _handleForegroundChangeDelegate, 0, 0, WINEVENT_OUTOFCONTEXT);

            _handleNameChangeDelegate = new NativeMethods.WinEventDelegate(HandleHookEventObjectNameChange);
            IntPtr hook2 = NativeMethods.SetWinEventHook(EVENT_OBJECT_NAMECHANGE, EVENT_OBJECT_NAMECHANGE, IntPtr.Zero, _handleNameChangeDelegate, 0, 0, WINEVENT_OUTOFCONTEXT);
        }

        void HandleHookEventSystemForegroundChange(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            var newMessage = new WindowEventHookMessage
            {
                Type = WindowEventHookMessageType.EventSystemForegroundChange,
                EventTime = dwmsEventTime,
                IdObject = idObject,
                WindowHandle = hwnd,
                EventType = eventType,
                IdChild = idChild
            };
            _internalPort.Post(newMessage);
        }

        void HandleHookEventObjectNameChange(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            var newMessage = new WindowEventHookMessage
            {
                Type = WindowEventHookMessageType.EventObjectNameChange,
                EventTime = dwmsEventTime,
                IdObject = idObject,
                WindowHandle = hwnd,
                EventType = eventType,
                IdChild = idChild,
            };
            _internalPort.Post(newMessage);
        }


        void HandleMessage(WindowEventHookMessage message)
        {
            try
            {
                if (message.EventTime < _lastEventTime)
                {
                    log.Warn("Detected instance of event time going backwards");
                }

                _lastEventTime = message.EventTime;

                MessageDetected?.Invoke(this, message);
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }
    }

}
