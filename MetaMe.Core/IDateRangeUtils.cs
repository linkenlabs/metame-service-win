using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaMe.Core
{
    static class IDateRangeUtils
    {
        //preconditions: items is sorted
        public static ImmutableArray<T> SkipTill<T>(this ImmutableArray<T> items, DateTime lowerBound) where T : IDateRange
        {
            var result = items.SkipWhile(item => item.Stop <= lowerBound).ToImmutableArray();
            return result;
        }
        public static ImmutableArray<T> Subtract<T>(T item, IDateRange range) where T : IDateRange
        {
            //range covers entire item
            if (range.Start <= item.Start
                && range.Stop >= item.Stop)
            {
                return ImmutableArray.Create<T>();
            }

            //out of range, leave untouched
            if (range.Stop <= item.Start
                || range.Start >= item.Stop)
            {
                var clone = item.Clone();
                return ImmutableArray.Create(clone);
            }

            //range trims the front
            if (range.Start <= item.Start
                && range.Stop < item.Stop
                && range.Stop > item.Start)
            {
                var clone = item.Clone();
                clone.Start = range.Stop;
                return ImmutableArray.Create(clone);
            }


            //range trims the back
            if (range.Start > item.Start
                && range.Start < item.Stop
                && range.Stop >= item.Stop)
            {
                var clone = item.Clone();
                clone.Stop = range.Start;
                return ImmutableArray.Create(clone);
            }

            //range splits the item
            if (range.Start > item.Start
                && range.Stop < item.Stop)
            {
                //front section
                var cloneA = item.Clone();
                cloneA.Stop = range.Start;

                var cloneB = item.Clone();
                cloneB.Start = range.Stop;

                return ImmutableArray.Create(cloneA, cloneB);
            }

            throw new NotImplementedException("Should not be here");
        }
        public static ImmutableArray<DateRange> Merge(this ImmutableArray<DateRange> dateRanges)
        {
            List<DateRange> sorted = (from range in dateRanges
                                      orderby range.Start ascending
                                      select range).ToList();

            List<DateRange> merged = new List<DateRange>();

            DateRange currentRange = null;

            foreach (var item in sorted)
            {
                if (currentRange == null)
                {
                    currentRange = new DateRange
                    {
                        Start = item.Start,
                        Stop = item.Stop
                    };

                    merged.Add(currentRange);
                    continue;
                }

                if (item.Start <= currentRange.Stop) //overlap case
                {
                    currentRange.Stop = item.Stop > currentRange.Stop ? item.Stop : currentRange.Stop;
                }
                else
                {
                    currentRange = new DateRange
                    {
                        Start = item.Start,
                        Stop = item.Stop
                    };

                    merged.Add(currentRange);
                    continue;
                }
            }

            return merged.ToImmutableArray();
        }
    }
}
