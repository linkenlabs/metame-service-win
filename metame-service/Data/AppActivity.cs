using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaMe.WindowsClient.Data
{
    //prevent obfuscation and renaming of variables
    class AppActivity
    {
        public int Id { get; set; }
        public int AppId { get; set; }

        public string Start { get; set; }
        public string Stop { get; set; }
    }
}
