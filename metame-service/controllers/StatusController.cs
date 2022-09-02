using System.Collections.Immutable;
using System.Web.Http;

namespace MetaMe.WindowsClient.controllers
{
    public class StatusController : ApiController
    {
        [HttpGet]
        public StatusInfo Get()
        {
            var initProgress = ClientApplication.Instance.InitializationProgress == null ? ImmutableArray.Create<LogInfo>() : ClientApplication.Instance.InitializationProgress.Logs.ToImmutableArray();

            return new StatusInfo
            {
                State = ClientApplication.Instance.Status.ToString(),
                AccountStatus = ClientApplication.Instance.AccountStatus.ToString(),
                InitializationProgress = initProgress,
                Exception = ClientApplication.Instance.Exception
            };
        }

    }
}
