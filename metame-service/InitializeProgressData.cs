using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MetaMe.WindowsClient
{
    class InitializeProgressData
    {
        public TaskCompletionSource<bool> TaskCompletionSource { get; private set; } = new TaskCompletionSource<bool>();
        public Exception Exception { get; set; }
        public List<LogInfo> Logs { get; private set; } = new List<LogInfo>();

        public void Log(string message)
        {
            Logs.Add(new LogInfo
            {
                Created = DateTime.UtcNow,
                Message = message
            });
        }
    }
}
