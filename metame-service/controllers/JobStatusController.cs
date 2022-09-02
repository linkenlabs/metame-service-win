using System;
using System.Web.Http;

namespace MetaMe.WindowsClient.controllers
{
    public class JobStatusController : ApiController
    {
        [HttpGet]
        [Route("~/api/jobStatus/{guid}")]
        public JobState Get(Guid guid)
        {
            var jobState = ClientApplication.Instance.GetJobState(guid);
            return jobState;
        }

    }
}
