using MetaMe.Core;

namespace MetaMe.WindowsClient.controllers
{
    public class GoalCreateRequest
    {
        public string Key { get; set; }
        public string Name { get; set; }
        public string Reason { get; set; }
        public GoalSubject Subject { get; set; }
        public string GoalType { get; set; }
        public int GoalValue { get; set; } //mins
        public string[] RepeatDaysOfWeek { get; set; }
    }
}
