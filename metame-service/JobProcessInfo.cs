using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaMe.WindowsClient
{
    public class JobProcessInfo
    {
        public ExportCsvRequest Request { get; set; }
        public JobState State { get; set; }
        public Guid Guid { get; set; }
    }
}
