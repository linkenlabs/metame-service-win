using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace MetaMe.WindowsClient.controllers
{
    public class Application2Controller : ApiController
    {

        [Route("~/api/application2")]
        [HttpGet]
        public ImmutableArray<string> Get(DateTime start, DateTime end)
        {
            return QueryUtils.GetAppNames(start, end, false);
        }
    }
}
