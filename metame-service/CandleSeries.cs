using MetaMe.WindowsClient.controllers;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaMe.WindowsClient
{
    public class CandleSeries
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public ImmutableArray<DateTimeValue> DataSeries { get; set; }
        public bool IsActive { get; set; }
        public DateTime LastUpdate { get; set; }
    }
}
