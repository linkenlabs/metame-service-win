using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace MetaMe.WindowsClient.controllers
{
    public class NotificationController : ApiController
    {
        [HttpGet]
        [Route("~/api/notification")]
        public IEnumerable<NotificationInfo> Get(DateTime lastQuery)
        {
            if (lastQuery.Kind == DateTimeKind.Local)
            {
                lastQuery = lastQuery.ToUniversalTime();
            }

            //filter only to new notifications after lastQuery
            var notificationsFiltered = (from item in ClientApplication.Instance.Notifications
                                         where item.Created > lastQuery
                                         select item);
            return notificationsFiltered;
        }
    }
}
