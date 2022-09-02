using log4net;
using MetaMe.Core;
using Microsoft.Win32;
using System;

namespace MetaMe.Sensors
{
    class PowerModeSensor
    {
        private PowerManagementEventEmitter _internalSensor;

        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public event EventHandler<IdleStateChanged> IdleStateChanged;

        bool _isSuspended = false;
        DateTime _suspendTime;

        public PowerModeSensor()
        {
            _internalSensor = new PowerManagementEventEmitter();
            _internalSensor.PowerModeChanged += _internalSensor_PowerModeChanged;
        }

        private void _internalSensor_PowerModeChanged(object sender, PowerManagementEventEnum e)
        {
            switch (e)
            {
                case PowerManagementEventEnum.EnteringSuspend:
                    _isSuspended = true;
                    _suspendTime = DateTime.UtcNow;
                    log.Debug("IdleStateChanged: Idle");

                    IdleStateChanged?.Invoke(this, new IdleStateChanged
                    {
                        IdleActivityInfo = null,
                        State = IdleStateEnum.Idle,
                        Timestamp = _suspendTime
                    });

                    return;

                case PowerManagementEventEnum.ResumeFromSuspend:

                    if (!_isSuspended)
                    {
                        return;
                    }

                    _isSuspended = false;
                    var resumeTime = DateTime.UtcNow;
                    var duration = resumeTime.Subtract(_suspendTime).TotalMilliseconds;
                    log.Debug("IdleStateChanged: Active");

                    IdleStateChanged?.Invoke(this, new IdleStateChanged
                    {
                        IdleActivityInfo = new IdleActivityInfo
                        {
                            Start = _suspendTime,
                            Stop = resumeTime,
                            Type = "Suspended"
                        },
                        State = IdleStateEnum.Active,
                        Timestamp = resumeTime
                    });

                    return;
                default:
                    return;
            }
        }
    }
}
