using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaMe.Core
{
    static class AppActivityInfoExtensions
    {
        public static ImmutableArray<ProcessActivityInfo> Subtract(this ProcessActivityInfo item, IEnumerable<DateRange> ranges)
        {
            if (ranges.Count() == 0)
            {
                return ImmutableArray.Create(item);
            }
            //possibility of overlapped ranges. Need to merge ranges first
            var mergedRanges = ranges.ToImmutableArray().Merge();

            List<ProcessActivityInfo> segments = new List<ProcessActivityInfo>();

            var temp = item.Clone();

            foreach (var range in mergedRanges)
            {
                //range is before the item
                if (range.Stop <= temp.Start)
                {
                    continue;
                }

                //range is past the item
                if (range.Start >= temp.Stop)
                {
                    continue;
                }

                //range covers the entire item
                if (range.Start <= temp.Start
                && range.Stop >= temp.Stop)
                {
                    //return the segments immediately. Nothing else to process;
                    return ImmutableArray.Create(segments.ToArray());
                }

                //range trims the front
                if (range.Start <= temp.Start
                    && range.Stop < temp.Stop
                    && range.Stop > temp.Start)
                {
                    temp.Start = range.Stop;
                }

                //range splits the item
                else if (range.Start > temp.Start
                    && range.Stop < temp.Stop)
                {
                    //front section
                    var cloneA = temp.Clone();
                    cloneA.Stop = range.Start;

                    segments.Add(cloneA);

                    //move start to the end of the range
                    temp.Start = range.Stop;
                }

                //range trims the back
                else if (range.Start > temp.Start
                    && range.Start < temp.Stop
                    && range.Stop >= temp.Stop)
                {
                    var clone = temp.Clone();
                    clone.Stop = range.Start;
                    segments.Add(clone);

                    return ImmutableArray.Create(segments.ToArray());
                }
            }

            //there was a bit left
            segments.Add(temp);

            return ImmutableArray.Create(segments.ToArray());
        }

        //Removes the range defined from the set
        public static ImmutableArray<ProcessActivityInfo> Subtract(this ImmutableArray<ProcessActivityInfo> items, ImmutableArray<DateRange> ranges)
        {
            List<ProcessActivityInfo> list = new List<ProcessActivityInfo>();

            foreach (var item in items)
            {
                list.AddRange(item.Subtract(ranges));
            }

            return list.ToImmutableArray();
        }

        public static ImmutableArray<ProcessActivityInfo> Subtract(this ImmutableArray<ProcessActivityInfo> items, DateRange range)
        {
            return items.Subtract(ImmutableArray.Create(range));
        }

        public static ImmutableArray<ProcessActivityInfo> Intersect(this ImmutableArray<ProcessActivityInfo> items, DateRange range)
        {

            List<ProcessActivityInfo> list = new List<ProcessActivityInfo>();

            foreach (var item in items)
            {
                if (item.Stop < range.Start)
                {
                    continue;
                }

                if (item.Start > range.Stop)
                {
                    continue;
                }

                ////trim all other cases
                //DateTime intersectStart = Max(item.Start, range.Start);
                //DateTime intersectStop = Min(item.Stop, range.Stop);

                list.Add(Intersect(item, range));

                //list.Add(new AppActivityInfo
                //{
                //    AppName = item.AppName,
                //    Start = intersectStart,
                //    Stop = intersectStop
                //});
            }

            return list.ToImmutableArray();
        }

        public static ProcessActivityInfo Intersect(this ProcessActivityInfo item, DateRange range)
        {
            if (item.Stop < range.Start)
            {
                throw new ArgumentOutOfRangeException();
            }

            if (item.Start > range.Stop)
            {
                throw new ArgumentOutOfRangeException();
            }

            //trim all other cases
            DateTime intersectStart = Max(item.Start, range.Start);
            DateTime intersectStop = Min(item.Stop, range.Stop);

            return new ProcessActivityInfo
            {
                AppName = item.AppName,
                Start = intersectStart,
                Stop = intersectStop
            };
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
