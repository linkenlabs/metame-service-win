using log4net;
using System;
using System.Collections.Generic;
using System.Management;

namespace MetaMe.Sensors
{
    class PowerManagementEventEmitter: IDisposable
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public event EventHandler<PowerManagementEventEnum> PowerModeChanged;

        private ManagementEventWatcher managementEventWatcher;

        private readonly Dictionary<string, string> powerValues = new Dictionary<string, string>
        {
            {"4", "Entering Suspend"},
            {"7", "Resume from Suspend"},
            {"10", "Power Status Change"},
            {"11", "OEM Event"},
            {"18", "Resume Automatic"}
        };

        public PowerManagementEventEmitter()
        {
            Start();
        }

        void Start()
        {
            var q = new WqlEventQuery();
            var scope = new ManagementScope("root\\CIMV2");

            q.EventClassName = "Win32_PowerManagementEvent";
            managementEventWatcher = new ManagementEventWatcher(scope, q);
            managementEventWatcher.EventArrived += PowerEventArrive;
            managementEventWatcher.Start();
        }

        private void PowerEventArrive(object sender, EventArrivedEventArgs e)
        {
            foreach (PropertyData pd in e.NewEvent.Properties)
            {
                if (pd == null || pd.Value == null) continue;

                if (powerValues.ContainsKey(pd.Value.ToString()))
                {
                    PowerManagementEventEnum powerManagementEvent = (PowerManagementEventEnum)Convert.ToInt32(pd.Value);
                    PowerModeChanged?.Invoke(this, powerManagementEvent);
                    log.DebugFormat("PowerModeChanged: {0}", powerManagementEvent);
                }
            }
        }

        public void Dispose()
        {
            managementEventWatcher.Stop();
            managementEventWatcher = null;
        }
    }
}
