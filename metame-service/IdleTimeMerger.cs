using MetaMe.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace MetaMe.WindowsClient
{
    class IdleTimeMerger
    {
        private ProcessActivityInfo _currentActivity;
        private IdleActivityInfo _currentIdle;

        private Queue<ProcessActivityInfo> _activitiesQueue;
        private Queue<IdleActivityInfo> _idleQueue;

        public IdleTimeMerger(ImmutableArray<ProcessActivityInfo> appActivities, ImmutableArray<IdleActivityInfo> idleActivities)
        {
            _activitiesQueue = new Queue<ProcessActivityInfo>(appActivities);
            _idleQueue = new Queue<IdleActivityInfo>(idleActivities);
        }

        public ImmutableArray<ProcessActivityInfo> PullAll()
        {
            List<ProcessActivityInfo> list = new List<ProcessActivityInfo>();
            ProcessActivityInfo item = null;
            while ((item = Pull()) != null)
            {
                list.Add(item);
            }
            return list.ToImmutableArray();
        }

        ProcessActivityInfo Pull()
        {
            var item = ReadNext();

            //handle end of queue
            if (item == null)
            {
                var emission = _currentActivity;
                _currentActivity = null;
                return emission;
            }

            //handle init
            if (_currentActivity == null
                && _currentIdle == null)
            {
                _currentActivity = item.Item1;
                _currentIdle = item.Item2;
                return Pull();
            }
            else if (item.Item1 != null)
            {
                return HandleActivity(item.Item1);

            }
            else if (item.Item2 != null) //handle idle
            {
                return HandleIdle(item.Item2);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        ProcessActivityInfo HandleIdle(IdleActivityInfo newItem)
        {
            if (_currentIdle != null
                && _currentActivity == null)
            {
                return HandleIdleThenIdle(newItem);
            }
            else if (_currentActivity != null
                && _currentIdle == null)
            {
                return HandleActivityThenIdle(newItem);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        ProcessActivityInfo HandleIdleThenIdle(IdleActivityInfo newItem)
        {
            //idle is behind
            if (newItem.Stop <= _currentIdle.Stop)
            {
                //do nothing
                return Pull();
            }
            //Handle idle overlapping with current
            else if (newItem.Start <= _currentIdle.Stop
                && newItem.Stop > _currentIdle.Stop)
            {
                //join them
                DateTime newStart = Min(newItem.Start, _currentIdle.Start);
                DateTime newStop = Max(newItem.Stop, _currentIdle.Stop);
                _currentIdle = new IdleActivityInfo
                {
                    Start = newStart,
                    Stop = newStop
                };
                return Pull();
            }
            //handle newIdle is non-continuous
            else if (newItem.Start > _currentIdle.Stop)
            {
                _currentIdle = newItem;
                return Pull();
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        ProcessActivityInfo HandleActivityThenIdle(IdleActivityInfo newItem)
        {
            if (newItem.Stop <= _currentActivity.Start)
            {
                return Pull();
            }
            else if (newItem.Start <= _currentActivity.Start //case B - idle trims activity start
                && newItem.Stop < _currentActivity.Stop)
            {
                _currentActivity = new ProcessActivityInfo
                {
                    Start = newItem.Stop,
                    Stop = _currentActivity.Stop,
                    AppName = _currentActivity.AppName
                };
                return Pull();
            }
            else if (newItem.Start > _currentActivity.Start //case C - idle is within appActivity
                && newItem.Stop < _currentActivity.Stop)
            {
                var emission = new ProcessActivityInfo
                {
                    Start = _currentActivity.Start,
                    Stop = newItem.Start,
                    AppName = _currentActivity.AppName
                };

                _currentActivity = new ProcessActivityInfo
                {
                    Start = newItem.Stop,
                    Stop = _currentActivity.Stop,
                    AppName = _currentActivity.AppName
                };
                return emission;
            }
            // case D - idle trims the back
            else if (newItem.Start > _currentActivity.Start
                && newItem.Start < _currentActivity.Stop
                && newItem.Stop >= _currentActivity.Stop)
            {
                var emission = new ProcessActivityInfo
                {
                    Start = _currentActivity.Start,
                    Stop = newItem.Start,
                    AppName = _currentActivity.AppName
                };

                _currentIdle = new IdleActivityInfo
                {
                    Start = _currentActivity.Stop,
                    Stop = newItem.Stop
                };
                _currentActivity = null;
                return emission;
            }
            //case E - idle is behind currentActivity
            else if (newItem.Start >= _currentActivity.Stop)
            {
                var emission = _currentActivity;
                _currentActivity = null;
                _currentIdle = newItem;
                return emission;
            }
            //case F - idle covers entire currentActivity
            else if (newItem.Start <= _currentActivity.Start
                && newItem.Stop >= _currentActivity.Stop)
            {
                _currentIdle = new IdleActivityInfo
                {
                    Start = _currentActivity.Stop,
                    Stop = newItem.Stop,
                };
                _currentActivity = null;
                return Pull();
            }
            else
            {
                throw new NotImplementedException();
            }

        }

        ProcessActivityInfo HandleActivity(ProcessActivityInfo newItem)
        {
            //we can assume that _currentActivity is !null
            if (_currentIdle == null
                && _currentActivity != null)
            {
                return HandleActivityThenActivity(newItem);
            }
            else if (_currentActivity == null
                && _currentIdle != null)
            {
                return HandleIdleThenActivity(newItem);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        ProcessActivityInfo HandleIdleThenActivity(ProcessActivityInfo newItem)
        {
            if (newItem.Stop <= _currentIdle.Stop)
            {
                return Pull();
            }
            //idle is finished
            else
            {
                //chop chop
                var newStart = Max(_currentIdle.Stop, newItem.Start);
                _currentActivity = new ProcessActivityInfo
                {
                    Start = newStart,
                    Stop = newItem.Stop,
                    AppName = newItem.AppName
                };
                _currentIdle = null;
                return Pull();
            }
        }

        //Handle _currentActivity followed by activity 
        ProcessActivityInfo HandleActivityThenActivity(ProcessActivityInfo newItem)
        {
            //handle newItem being entirely overlapped by currentItem
            if (newItem.Stop <= _currentActivity.Stop)
            {
                //Skip. Pull next 
                return Pull();
            }
            //handle newItem is after
            else
            {
                //trim 
                var emission = new ProcessActivityInfo
                {
                    AppName = _currentActivity.AppName,
                    Start = _currentActivity.Start,
                    Stop = _currentActivity.Stop
                };

                var newStart = Max(_currentActivity.Stop, newItem.Start);
                _currentActivity = new ProcessActivityInfo
                {
                    Start = newStart,
                    Stop = newItem.Stop,
                    AppName = newItem.AppName
                };
                return emission;
            }

        }

        Tuple<ProcessActivityInfo, IdleActivityInfo> ReadNext()
        {
            //peak and get the next event
            if (_activitiesQueue.Count == 0
                && _idleQueue.Count == 0)
            {
                return null;
            }

            //get the last activity
            if (_idleQueue.Count == 0)
            {
                var item = _activitiesQueue.Dequeue();
                return new Tuple<ProcessActivityInfo, IdleActivityInfo>(item, null);
            }

            //get the last idle
            if (_activitiesQueue.Count == 0)
            {
                var item = _idleQueue.Dequeue();
                return new Tuple<ProcessActivityInfo, IdleActivityInfo>(null, item);
            }

            //peek to see which one comes next
            var nextActivity = _activitiesQueue.Peek();
            var nextIdle = _idleQueue.Peek();

            if (nextActivity.Start < nextIdle.Start)
            {
                var item = _activitiesQueue.Dequeue();
                return new Tuple<ProcessActivityInfo, IdleActivityInfo>(item, null);
            }
            else
            {
                var item = _idleQueue.Dequeue();
                return new Tuple<ProcessActivityInfo, IdleActivityInfo>(null, item);
            }
        }
        static DateTime Max(DateTime a, DateTime b)
        {
            return a > b ? a : b;
        }

        static DateTime Min(DateTime a, DateTime b)
        {
            return a < b ? a : b;
        }



    }
}
