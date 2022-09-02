using MetaMe.Sensors;
using System;

namespace MetaMe.WindowsClient
{
    class AppActivityFactGeneratorEvent
    {
        public AppActivityEvent AppActivityEvent { get; set; }
        public IdleEvent IdleEvent { get; set; }

        public DateTime GetTimestamp()
        {
            if (AppActivityEvent != null)
            {
                return AppActivityEvent.Timestamp;
            }
            else if (IdleEvent != null)
            {
                return IdleEvent.Timestamp;
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}
