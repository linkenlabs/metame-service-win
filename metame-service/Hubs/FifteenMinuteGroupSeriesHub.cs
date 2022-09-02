using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaMe.WindowsClient.Hubs
{
    public class FifteenMinuteGroupSeriesHub: Hub
    {
        public ImmutableArray<CandleSeries> GetData()
        {
            return ClientApplication.Instance.GetFifteenMinuteCandleSeriesData();
        }
    }
}
