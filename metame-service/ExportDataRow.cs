using CsvHelper.Configuration.Attributes;
using System;

namespace MetaMe.WindowsClient
{
    public struct ExportDataRow
    {
        public DateTime DateTime { get; set; }
        public string AppName { get; set; }
        public string GroupName { get; set; }

        [Name("TotalActiveDuration(ms)")]
        public double TotalActiveDuration { get; set; }

        [Name("TotalIdleDuration(ms)")]
        public double TotalIdleDuration { get; set; }

        [Name("TotalDuration(ms)")]
        public double TotalDuration { get; set; }

        [Name("Frequency")]
        public int TotalItems { get; set; } //number of raw app activity items summarised within


    }
}
