using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaMe.WindowsClient
{
    class ApplicationScanInfo
    {
        public string ApplicationPath { get; set; }
        public string FileDescription { get; set; }
        public bool IsValidApp { get; set; } //checks if Application file path exists. Is not an uninstall file
    }
}
