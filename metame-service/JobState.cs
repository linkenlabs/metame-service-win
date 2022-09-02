using System;
using System.Collections.Immutable;

namespace MetaMe.WindowsClient
{
    public class JobState
    {
        public bool IsRunning { get; set; }
        public ImmutableArray<LogInfo> Output { get; set; } = ImmutableArray<LogInfo>.Empty;
        public Exception Exception { get; set; }
    }
}
