//using MetaMe.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace MetaMe.WindowsClient.controllers
{
    public class SettingsController : ApiController
    {
        public SettingsInfo Get()
        {
            var settingsInfo = ClientApplication.Instance.GetSettings();

            return new SettingsInfo
            {
                HasSeed = true,
                StartTime = settingsInfo.StartTime,
                StartDay = settingsInfo.StartDay
            };
        }

        public void Put(SettingsInfo info)
        {
            var settingsInfo = new Core.Setting
            {
                StartDay = info.StartDay,
                StartTime = info.StartTime
            };

            ClientApplication.Instance.SaveSettings(settingsInfo);
        }
    }
}
