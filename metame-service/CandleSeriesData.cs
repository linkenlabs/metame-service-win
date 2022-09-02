using MetaMe.Core;
using MetaMe.Sensors;
using MetaMe.WindowsClient.controllers;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaMe.WindowsClient
{
    public class CandleSeriesData
    {
        public ImmutableArray<CandleSeries> CandleSeries { get; set; }
    }
}
