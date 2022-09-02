using Newtonsoft.Json;
using System;
using System.Collections.Immutable;
using System.Net;
using System.Web.Http;

namespace MetaMe.WindowsClient.controllers
{
    public class SeriesController : ApiController
    {
        [Route("~/api/series")]
        [HttpGet]
        public ImmutableArray<DateTimeValue>[] GetSeries([FromUri] string[] groupName, [FromUri] string[] appName, DateTime start, int interval, int length)
        {
            //don't allow both group and appName query
            if (groupName.Length > 0 && appName.Length > 0)
            {
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }

            if (groupName.Length > 0)
            {
                return QueryUtils.GetGroupSeries2(groupName, start, interval, length);
            }
            else if (appName.Length > 0)
            {
                return QueryUtils.GetAppSeries(appName, start, interval, length);
            }
            else
            {
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }
        }

    }
}
