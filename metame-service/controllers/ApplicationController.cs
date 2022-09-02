using MetaMe.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Web.Http;

namespace MetaMe.WindowsClient.controllers
{
    public class ApplicationController : ApiController
    {
        [Route("~/api/application")]
        [HttpGet]
        public ImmutableArray<ApplicationInfo> Get(bool showHidden = false)
        {
            return ApplicationInfoUtils.GetApplicationInfo(showHidden);
        }

        [Route("~/api/application/{name}/hourLevelFacts")]
        [HttpGet]
        public IEnumerable<DateTimeValue> GetHourLevelFacts(string name, DateTime start, int limit, string propName = "TotalActiveDuration")
        {
            return QueryUtils.GetAppHourLevelFacts(name, start, limit, propName);
        }

        [Route("~/api/application/{name}/dayLevelFacts")]
        [HttpGet]
        public IEnumerable<DateTimeValue> GetDayLevelFacts(string name, string propName = "TotalActiveDuration")
        {
            return QueryUtils.GetAppDayLevelFacts(name, propName);
        }

        [Route("~/api/application/{name}/weekLevelFacts")]
        [HttpGet]
        public IEnumerable<DateTimeValue> GetWeekLevelFacts(string name, string propName = "TotalActiveDuration")
        {
            return QueryUtils.GetAppWeekLevelFacts(name, propName);
        }


        [Route("~/api/application/{*name}")]
        [HttpPost]
        public void GetApplication(string name)
        {
            //handle case of forward slashes not going through
            //https://github.com/aspnet/AspNetKatana/issues/208
            if (name.EndsWith("/setProductive"))
            {
                var appName = name.Replace("/setProductive", "");
                SetProductive(appName);
            }
            else if (name.EndsWith("/setNeutral"))
            {
                var appName = name.Replace("/setNeutral", "");
                SetNeutral(appName);

            }
            else if (name.EndsWith("/setUnproductive"))
            {
                var appName = name.Replace("/setUnproductive", "");
                SetUnproductive(appName);

            }
            else if (name.EndsWith("/hide"))
            {
                var appName = name.Replace("/hide", "");
                SetHidden(appName);
            }
            else
            {
                throw new HttpResponseException(System.Net.HttpStatusCode.NotFound);
            }
        }

        [Route("~/api/application/{name}/setNeutral")]
        [HttpPost]
        public void SetNeutral(string name)
        {
            var newSet = ClientApplication.Instance.GetGroups().RemoveApp(name);
            ClientApplication.Instance.SetGroups(newSet);
        }

        [Route("~/api/application/{name}/setProductive")]
        [HttpPost]
        public void SetProductive(string name)
        {
            AddAppToGroup(name, "Productive");
        }

        [Route("~/api/application/{name}/setUnproductive")]
        [HttpPost]
        public void SetUnproductive(string name)
        {
            AddAppToGroup(name, "Unproductive");
        }

        [Route("~/api/application/{name}/hide")]
        [HttpPost]
        public void SetHidden(string name)
        {
            //remove app from productive and unproductive if they exist
            var relevantGroups = (from g in ClientApplication.Instance.GetGroups()
                                  where g.Items.Contains(name)
                                  select g).ToList();

            if (relevantGroups.Count > 0)
            {
                var newSet = ClientApplication.Instance.GetGroups().RemoveApp(name);
                ClientApplication.Instance.SetGroups(newSet);
            }

            //add to hiddenList
            var hiddenAppList = ClientApplication.Instance.GetHiddenAppList();
            //do nothing if already in the list
            if (hiddenAppList.Items.Contains(name))
            {
                return;
            }

            var newHiddenList = new HiddenAppList
            {
                Items = hiddenAppList.Items.Append(name).ToArray()
            };
            ClientApplication.Instance.SaveHiddenAppList(newHiddenList);
        }

        void AddAppToGroup(string appName, string groupName)
        {
            var newSet = ClientApplication.Instance.GetGroups().RemoveApp(appName);
            var relevantGroup = (from item in newSet
                                 where item.Name == groupName
                                 select item).First();

            relevantGroup.Items = relevantGroup.Items.Append(appName).ToArray();
            ClientApplication.Instance.SetGroups(newSet);
        }


    }
}
