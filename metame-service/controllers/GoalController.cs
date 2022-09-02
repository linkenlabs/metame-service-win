using MetaMe.Core;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Web.Http;

namespace MetaMe.WindowsClient.controllers
{
    public class GoalController : ApiController
    {
        [HttpGet]
        [Route("~/api/goal")]
        public ImmutableArray<Goal> Get()
        {
            return ClientApplication.Instance.GetGoals();
        }

        [HttpGet]
        [Route("~/api/goal/{key}")]
        public Goal GetByGuid(string key)
        {
            var matching = (from item in ClientApplication.Instance.GetGoals()
                            where item.Key == key
                            select item).FirstOrDefault();

            return matching;
        }

        [HttpGet]
        [Route("~/api/goal/{key}/goalPerformance")]
        public ImmutableArray<GoalPerformanceWeek> GetGoalMap(string key)
        {
            //get goal
            var goal = (from item in ClientApplication.Instance.GetGoals()
                        where item.Key == key
                        select item).FirstOrDefault();

            if (goal == null)
            {
                return ImmutableArray.Create<GoalPerformanceWeek>();
            }

            var dayLevelFacts = QueryUtils.GetDayLevelFacts(goal.Subject.Type, goal.Subject.Name);

            var startOfWeek = QueryUtils.GetUtcStartDayOfWeek();
            var startOfDay = QueryUtils.GetUtcStartTimeOfDay();
            var utcDayStart = DateTime.UtcNow.ToDayStart(startOfDay);

            Func<DateTime, double, bool> predicate = (date, value) =>
             {
                 return value > goal.GoalValue * 60 * 1000;
             };

            if (goal.GoalType == "lessThan")
            {
                //if timeRemaing exceeds time left to make the goal then skip
                predicate = (date, value) =>
                {
                    //goals in the future can't be met yet
                    if (date > utcDayStart)
                    {
                        return false;
                    }
                    //if the date is today, account for time remaining in the day
                    else if (date == utcDayStart)
                    {
                        var timeRemainingToday = utcDayStart.AddDays(1) - DateTime.UtcNow;
                        var progressInMinutes = TimeSpan.FromMilliseconds(value).TotalMinutes;
                        return timeRemainingToday < TimeSpan.FromMinutes(goal.GoalValue - progressInMinutes);
                    }
                    else
                    {
                        return value < goal.GoalValue * 60 * 1000;
                    }

                };
            }

            //Group into weeks
            var result = (from item in dayLevelFacts
                          group item by item.DateTime.ToWeekStart(startOfWeek, startOfDay) into g
                          select new
                          {
                              WeekStart = g.Key,
                              Items = g.ToList()
                          }).ToList();

            var goalPerformance = result.ConvertAll((weekItem) =>
            {

                var dayPerformances = Enumerable.Range(0, 7).ToList().ConvertAll((index) =>
                {
                    DateTime dayStart = weekItem.WeekStart.AddDays(index);
                    var match = weekItem.Items.FirstOrDefault((d) => d.DateTime == dayStart);
                    var value = match != null ? match.Value : 0;
                    var hasGoal = goal.RepeatDaysOfWeek.Contains(dayStart.ToLocalTime().DayOfWeek.ToString());

                    return new GoalPerformanceDay
                    {
                        DateTime = dayStart,
                        Value = value,
                        IsGoalMet = predicate(dayStart, value),
                        HasGoal = hasGoal
                    };
                });

                return new GoalPerformanceWeek
                {
                    WeekStart = weekItem.WeekStart,
                    DayPerformance = dayPerformances.ToImmutableArray()
                };
            });

            return goalPerformance.ToImmutableArray();
        }

        [HttpPut]
        [Route("~/api/goal/{key}")]
        public void Put(string key, GoalPutRequest request)
        {
            var goals = ClientApplication.Instance.GetGoals();

            var matching = (from item in goals
                            where item.Key == key
                            select item).First();

            matching.Name = request.Name;
            matching.Reason = request.Reason;
            matching.Subject = request.Subject;
            matching.RepeatDaysOfWeek = request.RepeatDaysOfWeek;
            matching.GoalType = request.GoalType;
            matching.GoalValue = request.GoalValue;

            ClientApplication.Instance.SetGoals(goals);
        }

        [HttpPost]
        [Route("~/api/goal")]
        public void Post(GoalCreateRequest request)
        {
            //validate
            if (request.Subject == null
                || String.IsNullOrEmpty(request.Name))
            {
                return;
            }

            Goal newItem = new Goal
            {
                Key = request.Key,
                Created = DateTime.UtcNow,
                GoalType = request.GoalType,
                GoalValue = request.GoalValue,
                Name = request.Name,
                Reason = request.Reason,
                Subject = request.Subject,
                RepeatDaysOfWeek = request.RepeatDaysOfWeek
            };

            ImmutableArray<Goal> data = ClientApplication.Instance.GetGoals();
            data = data.Add(newItem);
            ClientApplication.Instance.SetGoals(data);
        }

        [HttpDelete]
        [Route("~/api/goal/{key}")]
        public void Delete(string key)
        {

            var matchingGoal = (from item in ClientApplication.Instance.GetGoals()
                                where item.Key == key
                                select item).FirstOrDefault();

            if (matchingGoal == null)
            {
                return;
            }

            ImmutableArray<Goal> temp = ClientApplication.Instance.GetGoals();
            temp = temp.Remove(matchingGoal);
            ClientApplication.Instance.SetGoals(temp);
        }
    }
}
