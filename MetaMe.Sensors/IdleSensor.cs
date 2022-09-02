using log4net;
using MetaMe.Core;
using Microsoft.Ccr.Core;
using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace MetaMe.Sensors
{
    class IdleSensor
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public event EventHandler<IdleStateChanged> IdleStateChanged;

        private Timer _timer;
        private bool _isIdle;
        public const int IDLE_THRESHOLD_MS = 60 * 1000;
        private DateTime _idleStart;
        private Port<IdleSensorRequest> _messagePort = new Port<IdleSensorRequest>();

        public IdleSensor(DispatcherQueue queue)
        {
            Arbiter.Activate(queue,
            Arbiter.Interleave(
                new TeardownReceiverGroup(),
                new ExclusiveReceiverGroup(
                    Arbiter.Receive(true, _messagePort, HandleRequest)),
                new ConcurrentReceiverGroup()));
        }

        public void Start()
        {
            _timer = new Timer(state => _messagePort.Post(new IdleSensorRequest()));
            _timer.Change(IDLE_THRESHOLD_MS, 1000);
        }

        public void Reset()
        {
            if (_timer == null)
            {
                return;
            }
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
            _timer = null;
            IdleStateChanged = null;
        }

        void HandleRequest(IdleSensorRequest request)
        {
            //last input time in ticks since system start
            uint lastInputTime = GetLastInputTime();

            ulong tickCount = NativeMethods.GetTickCount64();
            //idleTime in milliseconds
            var idleTime = Convert.ToInt64(tickCount) - Convert.ToInt64(lastInputTime);

            //if not idle, and over threshold then switch to idle
            if (!_isIdle && idleTime > IDLE_THRESHOLD_MS)
            {
                _isIdle = true;
                _idleStart = DateTime.UtcNow;

                OnIdleStateChanged(new IdleStateChanged
                {
                    IdleActivityInfo = null,
                    State = IdleStateEnum.Idle,
                    Timestamp = _idleStart
                });
            }
            //if idle and less than threshold then switch to active
            else if (_isIdle && idleTime < IDLE_THRESHOLD_MS)
            {
                _isIdle = false;
                DateTime idleStop = DateTime.UtcNow;

                OnIdleStateChanged(new IdleStateChanged
                {
                    IdleActivityInfo = new IdleActivityInfo
                    {
                        Start = _idleStart,
                        Stop = idleStop
                    },
                    State = IdleStateEnum.Active,
                    Timestamp = idleStop
                });
            }
        }

        void OnIdleStateChanged(IdleStateChanged state)
        {
            IdleStateChanged?.Invoke(this, state);
            log.Debug("IdleStateChanged emitted");
            log.Debug(state);
        }

        uint GetLastInputTime()
        {
            NativeMethods.LASTINPUTINFO lastInPut = new NativeMethods.LASTINPUTINFO();
            lastInPut.cbSize = (uint)Marshal.SizeOf(lastInPut);
            if (!NativeMethods.GetLastInputInfo(ref lastInPut))
            {
                throw new Exception(NativeMethods.GetLastError().ToString());
            }

            return lastInPut.dwTime;
        }
    }
}
