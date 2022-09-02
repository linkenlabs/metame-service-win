using MetaMe.Core;
using MetaMe.WindowsClient.controllers;
using Microsoft.Ccr.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace MetaMe.WindowsClient
{
    class NotificationGenerator
    {
        private readonly Timer _refreshTimer;
        public const int INTERVAL_MS = 5 * 1000;
        readonly Port<EmptyValue> _port = new Port<EmptyValue>();
        private ImmutableArray<GoalProgressNotification> _emittedNotifications = ImmutableArray<GoalProgressNotification>.Empty;

        public NotificationGenerator(DispatcherQueue queue, ClientApplication clientApp)
        {
            Arbiter.Activate(queue,
             Arbiter.Interleave(
                 new TeardownReceiverGroup(),
                 new ExclusiveReceiverGroup(
                     Arbiter.Receive(true, _port, Generate)),
                 new ConcurrentReceiverGroup()
          ));

            _refreshTimer = new Timer((object state) =>
            {
                if (clientApp.Status != ClientApplicationStatus.Started)
                {
                    return;
                }
                _port.Post(EmptyValue.SharedInstance);
            }, null, INTERVAL_MS, INTERVAL_MS);
        }

        void Generate(EmptyValue state)
        {
            //Don't check goals until everything is initialized
            if (ClientApplication.Instance.Status != ClientApplicationStatus.Started)
            {
                return;
            }

            var progressTargets = new double[] { 0.8, 1 };
            var goals = ClientApplication.Instance.GetGoals();

            var utcStartTime = ClientApplication.Instance.GetUtcStartTimeOfDay();
            var dayStart = DateTime.UtcNow.ToDayStart(utcStartTime);

            List<GoalProgressNotification> newNotifications = new List<GoalProgressNotification>();

            foreach (var goal in goals)
            {
                var progress = GetGoalProgress(goal, dayStart);

                foreach (var target in progressTargets)
                {
                    if (IsNotificationRaised(goal, target, dayStart, _emittedNotifications))
                    {
                        continue;
                    }

                    if ((target == 1 && progress > target)
                        || (target < 1 && progress < 1 && progress > target))
                    {
                        Console.WriteLine("goal:{0}, target:{1}, progress:{2}", goal.Name, target, progress);
                        //raise the target
                        newNotifications.Add(new GoalProgressNotification
                        {
                            GoalValue = goal.GoalValue,
                            GoalKey = goal.Key,
                            Progress = progress,
                            ProgressTarget = target,
                            Created = DateTime.UtcNow
                        });
                    }
                }
            }

            //nothing else to do
            if (newNotifications.Count == 0)
            {
                return;
            }

            _emittedNotifications = _emittedNotifications.AddRange(newNotifications);

            var converted = newNotifications.ConvertAll(ConvertToNotificationInfo);

            ClientApplication.Instance.Notifications = ClientApplication.Instance.Notifications.AddRange(converted);
        }
        static NotificationInfo ConvertToNotificationInfo(GoalProgressNotification item)
        {
            var data = new
            {
                Key = item.GoalKey,
                Name = item.GoalKey,
                item.ProgressTarget,
                item.Progress,
                Value = item.GoalValue
            };

            var itemCreated = item.Created;
            var created = new DateTime(itemCreated.Year, itemCreated.Month, itemCreated.Day, itemCreated.Hour, itemCreated.Minute, itemCreated.Second, itemCreated.Kind);
            var type = item.ProgressTarget < 1 ? "GoalProgress" : "GoalAchieved";

            var notification = new NotificationInfo
            {
                Type = type,
                Data = data,
                Created = created
            };
            return notification;
        }

        public static double GetGoalProgress(Goal goal, DateTime utcDayStart)
        {
            var dayLevelFacts = QueryUtils.GetDayLevelFacts(goal.Subject.Type, goal.Subject.Name);

            var matchingDayLevelFact = (from item in dayLevelFacts
                                        where item.DateTime == utcDayStart
                                        select item).FirstOrDefault();

            if (matchingDayLevelFact == null)
            {
                return 0;
            }

            var goalProgressMins = TimeSpan.FromMilliseconds(matchingDayLevelFact.Value).TotalMinutes;
            return goalProgressMins / goal.GoalValue;
        }
        public static bool IsNotificationRaised(Goal goal, double progressTarget, DateTime utcDayStart, ImmutableArray<GoalProgressNotification> notificationHistory)
        {
            //get the notifications raised today
            var startTime = ClientApplication.Instance.GetUtcStartTimeOfDay();

            var matchingNotification = (from n in notificationHistory
                                        where n.Created.ToDayStart(startTime) == utcDayStart
                                        && n.ProgressTarget == progressTarget
                                        && n.GoalKey == goal.Key
                                        && n.GoalValue == goal.GoalValue
                                        select n).FirstOrDefault();

            return matchingNotification != null;
        }
    }
}
