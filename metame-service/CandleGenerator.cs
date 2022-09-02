using log4net;
using MetaMe.Core;
using MetaMe.WindowsClient.controllers;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace MetaMe.WindowsClient
{
    class CandleGenerator
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public event EventHandler<CandleSeriesData> CandlesChanged;
        readonly string[] RESERVED_GROUPS = new string[] { "Productive", "Unproductive", "Total" };

        ImmutableArray<CandleSeries> _candleSeries = ImmutableArray.Create<CandleSeries>();
        private readonly int _periodMinutes;
        private readonly int _periodLimit;
        //create after hour level facts have been generated
        public CandleGenerator(int periodMinutes, int limit)
        {
            _periodMinutes = periodMinutes;
            _periodLimit = limit;
        }

        public ImmutableArray<CandleSeries> GetCandleSeriesData()
        {
            return _candleSeries;
        }

        //Refreshes all candles from scratch
        public void Refresh()
        {
            log.Debug("Refreshing...");
            var candleSeries = CalculateInitialSeries();
            _candleSeries = candleSeries;
            OnCandlesChanged(candleSeries);
        }

        ImmutableArray<CandleSeries> CalculateInitialSeries()
        {
            var upperBound = DateTime.UtcNow.ToPeriodStart(_periodMinutes).AddMinutes(_periodMinutes);
            var lowerBound = upperBound.AddMinutes(-1 * (_periodLimit - 1) * _periodMinutes);

            //skip for performance reasons
            ImmutableArray<ProcessActivityInfo> activities = ClientApplication.Instance.GetAppActivityInfo().SkipTill(lowerBound);
            ImmutableArray<IdleActivityInfo> idleActivities = ClientApplication.Instance.GetIdleActivityInfo().SkipTill(lowerBound);

            var subtractRange = new DateRange
            {
                Start = DateTime.MinValue,
                Stop = lowerBound
            };

            var mostRecentActivity = new ProcessActivityInfo
            {
                Start = ClientApplication.Instance.ActiveForeground.Start,
                AppName = ClientApplication.Instance.ActiveForeground.AppName,
                Stop = DateTime.MaxValue
            };

            var adjustedActivities = activities.Subtract(subtractRange).Add(mostRecentActivity);
            var adjustedIdleActivities = idleActivities.Subtract(subtractRange);

            var periodStart = AppActivityFactUtils.GetPeriodStart(adjustedActivities, _periodMinutes);
            var result = AppActivityFactUtils.CalculateNextState(periodStart, adjustedActivities, adjustedIdleActivities, _periodMinutes);
            var hiddenList = ClientApplication.Instance.GetHiddenAppList();

            var candleSeries = RESERVED_GROUPS.Select(groupName =>
            {
                return CandleUtils.ConvertToCandleSeries(groupName, _periodLimit, _periodMinutes, ImmutableArray.Create<DateTimeValue>(), result.Item2, result.Item1, hiddenList.Items);
            }).ToImmutableArray();

            return candleSeries;
        }

        public void UpdateCandles(ImmutableArray<AppActivityFact> appActivityFacts, AppActivityFactGeneratorState state)
        {
            if (_candleSeries.Length == 0)
            {
                _candleSeries = CalculateInitialSeries();
            }
            var hiddenList = ClientApplication.Instance.GetHiddenAppList();
            _candleSeries = _candleSeries.Select(d => CandleUtils.ConvertToCandleSeries(d.Name, _periodLimit, _periodMinutes, d.DataSeries, appActivityFacts, state, hiddenList.Items)).ToImmutableArray();
            OnCandlesChanged(_candleSeries);
        }

        public void Reset()
        {
            _candleSeries = ImmutableArray.Create<CandleSeries>();
            CandlesChanged = null;
        }

        void OnCandlesChanged(ImmutableArray<CandleSeries> candles)
        {
            log.Debug("Candle changes emitted...");
            var candleSeriesData = new CandleSeriesData
            {
                CandleSeries = candles
            };
            CandlesChanged?.Invoke(this, candleSeriesData);
        }

    }
}
