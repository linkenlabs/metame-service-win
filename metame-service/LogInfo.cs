using System;

namespace MetaMe.WindowsClient
{
    public class LogInfo
    {
        public DateTime Created { get; set; }
        public string Message { get; set; }
        public string Status { get; set; } //OK, FAILED, NOTHING, %

        public static LogInfo Create(string message)
        {
            return new LogInfo
            {
                Created = DateTime.UtcNow,
                Message = message
            };
        }
    }
}
