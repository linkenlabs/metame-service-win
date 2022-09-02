using MetaMe.Core;
using System.Web.Http;

namespace MetaMe.WindowsClient.controllers
{
    public class HiddenAppListController: ApiController
    {
        public HiddenAppList Get()
        {
            var hiddenAppList = ClientApplication.Instance.GetHiddenAppList();

            return hiddenAppList;
        }

        public void Put(HiddenAppList item)
        {
            ClientApplication.Instance.SaveHiddenAppList(item);
        }
    }
}
