namespace MetaMe.WindowsClient
{
    public class ExportCsvRequest
    {
        public int Granularity { get; set; } //in minutes
        public int TimePeriod { get; set; } //in days
        public string OutputPath { get; set; }

    }
}
