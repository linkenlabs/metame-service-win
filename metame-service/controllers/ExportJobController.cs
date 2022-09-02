using System;
using System.Web.Http;

namespace MetaMe.WindowsClient.controllers
{
    public class ExportJobController : ApiController
    {
        [HttpPost]
        [Route("~/api/exportjob")]

        public Guid Post(ExportCsvRequest request)
        {
            //create new guid
            //put in list create status
            //start job, pass in  status object ref
            return ClientApplication.Instance.Export(request);
        }
    }
}
