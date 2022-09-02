using MetaMe.Core;
using System;
using System.Threading.Tasks;
using System.Web.Http;

namespace MetaMe.WindowsClient.controllers
{
    public class AccountController : ApiController
    {
        [Route("~/api/account")]
        [HttpGet]
        public AccountInfo GetAccountInfo()
        {
            //if none
            var appActivityInfo = ClientApplication.Instance.GetAppActivityInfo();
            if (appActivityInfo.Length == 0)
            {
                return new AccountInfo
                {
                    Created = ClientApplication.Instance.ActiveForeground.Start
                };
            }

            return new AccountInfo
            {
                Created = appActivityInfo[0].Start
            };
        }

        [Route("~/api/account/logout")]
        [HttpPost]
        public Task<bool> Logout()
        {
            return Task.Run(async () =>
            {
                await ClientApplication.Instance.Stop();
                ClientApplication.Instance.Delete();
                return true;
            });
        }
    };
}
