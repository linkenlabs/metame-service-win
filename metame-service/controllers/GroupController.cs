using MetaMe.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Web;
using System.Web.Http;

namespace MetaMe.WindowsClient.controllers
{
    public class GroupController : ApiController
    {
        [HttpGet]
        [Route("~/api/group")]
        public ImmutableArray<Group> Get()
        {
            ImmutableArray<Group> data = ClientApplication.Instance.GetGroups();
            return data;
        }

        [HttpGet]
        [Route("~/api/group/{name}")]
        public Group GetByName(string name)
        {
            return QueryUtils.GetByName(name);
        }

        [Route("~/api/group/{name}/mostUsedApps")]
        [HttpGet]
        public IEnumerable<NameValue> GetMostUsedApps(string name, int limit, DateTime? date = null)
        {
            Group groupItem = GetByName(name);

            var hourLevelFacts = ClientApplication.Instance.GetHourLevelFacts();

            //filter between dates if date exists
            if (date.HasValue)
            {
                DateTime start = date.Value;
                //convert if not utc
                if (start.Kind == DateTimeKind.Local)
                {
                    start = start.ToUniversalTime().ToPeriodStart(60);
                }
                DateTime end = start.AddHours(24);

                hourLevelFacts = (from item in hourLevelFacts
                                  where item.DateTime >= start && item.DateTime < end
                                  select item).ToImmutableArray();

            }

            var durationByAppName = (from item in hourLevelFacts
                                     where groupItem.Items.Contains(item.AppName)
                                     group item by item.AppName into g
                                     select new
                                     {
                                         AppName = g.Key,
                                         Duration = g.Sum(i => i.TotalActiveDuration)
                                     }).ToList();

            var nameValues = (from item in durationByAppName
                              orderby item.Duration descending
                              select new NameValue
                              {
                                  Name = item.AppName,
                                  Value = item.Duration
                              }).Take(Math.Min(limit, durationByAppName.Count));

            return nameValues;
        }

        [Route("~/api/group/{name}/hourLevelFacts")]
        [HttpGet]
        public IEnumerable<DateTimeValue> GetHourLevelFacts(string name, DateTime start, int limit, string propName = "TotalActiveDuration")
        {
            return QueryUtils.GetGroupHourLevelFacts(name, start, limit, propName);
        }

        [Route("~/api/group/{name}/dayLevelFacts")]
        [HttpGet]
        public IEnumerable<DateTimeValue> GetDayLevelFacts(string name, string propName = "TotalActiveDuration")
        {
            return QueryUtils.GetGroupDayLevelFacts(name, propName);
        }

        [Route("~/api/group/{name}/weekLevelFacts")]
        [HttpGet]
        public IEnumerable<DateTimeValue> GeWeekLevelFacts(string name, string propName = "TotalActiveDuration")
        {
            return QueryUtils.GeGroupWeekLevelFacts(name, propName);
        }


        [HttpPut]
        [Route("~/api/group")]
        public void EditGroup(Group putGroupRequest)
        {
            if (putGroupRequest.Items == null)
            {
                return;
            }

            var groups = ClientApplication.Instance.GetGroups();

            Group matchingRecord = (from g in groups
                                    where g.Name == putGroupRequest.Name
                                    select g).FirstOrDefault();

            if (matchingRecord == null)
            {
                throw new HttpResponseException(System.Net.HttpStatusCode.BadRequest);
            }

            //remove all groups from others
            foreach (var group in groups)
            {
                if (group.Name == putGroupRequest.Name)
                {
                    group.Items = putGroupRequest.Items;
                }
                else
                {
                    group.Items = group.Items.Except(putGroupRequest.Items).ToArray();
                }
            }
            ClientApplication.Instance.SetGroups(groups);

        }
    }
}
