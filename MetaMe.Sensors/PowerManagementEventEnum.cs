using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaMe.Sensors
{

    enum PowerManagementEventEnum
    {
        //https://blogs.technet.microsoft.com/heyscriptingguy/2011/08/16/monitor-and-respond-to-windows-power-events-with-powershell/
        EnteringSuspend = 4,
        ResumeFromSuspend = 7,
        PowerStatusChange = 10,
        OemEvent = 11,
        ResumeAutomatic = 18
    }
}
