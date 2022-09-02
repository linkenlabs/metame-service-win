using System;
using System.Collections.Immutable;

namespace MetaMe.WindowsClient.controllers
{
    public class StatusInfo
    {
        public string State { get; set; }
        public string AccountStatus { get; set; }
        public ImmutableArray<LogInfo> InitializationProgress { get; set; }
        public Exception Exception { get; set; }
    }
}
