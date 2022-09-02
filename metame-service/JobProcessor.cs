using CsvHelper;
using MetaMe.Core;
using Microsoft.Ccr.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;

namespace MetaMe.WindowsClient
{
    public class JobProcessor
    {
        private readonly Dictionary<Guid, JobProcessInfo> _jobs = new Dictionary<Guid, JobProcessInfo>();
        private readonly Port<JobProcessInfo> _port = new Port<JobProcessInfo>();
        public JobProcessor(DispatcherQueue queue)
        {
            Arbiter.Activate(queue,
                Arbiter.Interleave(
                    new TeardownReceiverGroup(),
                    new ExclusiveReceiverGroup(
                        Arbiter.Receive(true, _port, HandleJobProcessInfo)),
                    new ConcurrentReceiverGroup()));
        }

        public JobProcessInfo GetJobProcessInfo(Guid guid)
        {
            if (!_jobs.ContainsKey(guid))
            {
                return null;
            }

            return _jobs[guid];
        }
        public Guid Process(ExportCsvRequest request)
        {
            Guid newGuid = Guid.NewGuid();
            JobProcessInfo info = new JobProcessInfo
            {
                Guid = newGuid,
                Request = request,
                State = new JobState
                {
                    IsRunning = true
                }
            };
            _jobs.Add(newGuid, info);
            _port.Post(info);
            return newGuid;
        }

        void HandleJobProcessInfo(JobProcessInfo request)
        {
            Action<string> log = (message) =>
            {
                request.State.Output = request.State.Output.Add(LogInfo.Create(message));
            };

            //intervalMins
            var interval = request.Request.Granularity;
            //var dayMinutes = 24 * 60;

            try
            {

                //get period start for all time
                var periodEnd = DateTime.UtcNow.ToPeriodStart(interval).AddMinutes(interval); //round to nearest interval
                var periodStart = ClientApplication.Instance.GetAppActivityInfo().First().Start.ToPeriodStart(interval);
                //var limit = (dayMinutes / interval) * request.Request.TimePeriod;
                if (request.Request.TimePeriod > 0)
                {
                    periodStart = periodEnd.AddDays(-1 * request.Request.TimePeriod);
                }

                log("Preparing data...");
                ImmutableArray<ProcessActivityInfo> activities = ClientApplication.Instance.GetAppActivityInfo().SkipTill(periodStart);
                ImmutableArray<IdleActivityInfo> idleActivities = ClientApplication.Instance.GetIdleActivityInfo().SkipTill(periodStart);

                //Trim activities and idleActivities to hourStart
                var subtractRange = new DateRange
                {
                    Start = DateTime.MinValue,
                    Stop = periodStart
                };

                log("Calculating time series...");
                var adjustedActivities = activities.Subtract(subtractRange);
                var adjustedIdleActivities = idleActivities.Subtract(subtractRange);
                periodStart = AppActivityFactUtils.GetPeriodStart(adjustedActivities, interval);

                var result = AppActivityFactUtils.CalculateNextState(periodStart, adjustedActivities, adjustedIdleActivities, interval);
                //also calculate partial facts without delta
                var partialFacts = AppActivityFactUtils.CalculateAppActivityFacts(result.Item1.ActivitiesBuffer, result.Item1.IdleActivitiesBuffer, interval).ToImmutableArray();

                var allFacts = result.Item2.AddRange(partialFacts);

                log("Joining group data...");
                var groups = ClientApplication.Instance.GetGroups();

                var rows = allFacts.ToList().ConvertAll((fact) =>
                {
                    var matchingGroup = groups.FirstOrDefault(g => g.Items.Contains(fact.AppName));

                    return new ExportDataRow
                    {
                        AppName = fact.AppName,
                        DateTime = fact.DateTime,
                        GroupName = matchingGroup == null ? string.Empty : matchingGroup.Name,
                        TotalActiveDuration = Math.Round(fact.TotalActiveDuration, MidpointRounding.AwayFromZero),
                        TotalDuration = Math.Round(fact.TotalDuration, MidpointRounding.AwayFromZero),
                        TotalIdleDuration = Math.Round(fact.TotalIdleDuration, MidpointRounding.AwayFromZero),
                        TotalItems = fact.TotalItems
                    };

                });
                //create export folder if now existing
                var directory = Path.GetDirectoryName(request.Request.OutputPath);
                Directory.CreateDirectory(directory);

                using (var writer = new StreamWriter(request.Request.OutputPath))
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    csv.WriteRecords(rows);
                }

                log("Export completed");
            }
            catch (Exception ex)
            {
                request.State.Exception = ex;
                log(string.Format("Exception: {0}", ex.ToString()));
            }
            finally
            {
                request.State.IsRunning = false;
            }
        }

    }
}
