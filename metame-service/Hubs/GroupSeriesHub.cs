using Microsoft.AspNet.SignalR;
using System.Collections.Immutable;

namespace MetaMe.WindowsClient.Hubs
{
    public class GroupSeriesHub : Hub
    {
        public ImmutableArray<CandleSeries> GetData()
        {
            return ClientApplication.Instance.GetHourCandleSeriesData();
        }
    }
}
