using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaMe.WindowsClient.controllers
{
    class JobStatusInfo
    {
        public Guid JobId { get; set; }
        public float Progress { get; set; } // 0 to 1
        public Exception Exception { get; set; }
        public string State { get; set; } //completed, running, error
        public LogInfo[] Output { get; set; }

    }
}
