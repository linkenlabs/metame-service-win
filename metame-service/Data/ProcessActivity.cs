using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaMe.WindowsClient.Data
{
    class ProcessActivity
    {
        public int Id { get; set; }
        public int ProcessId { get; set; }
        public string AddressUrl { get; set; } //only filled when browser
        public string WindowTitle { get; set; } //always filled
    }
}
