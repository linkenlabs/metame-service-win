using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaMe.Core
{
    interface IDateRange
    {
        DateTime Start { get; set; }
        DateTime Stop { get; set; }
    }
}
