using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaMe.Core
{
    static class IdleActivityInfoExtensions
    {
        public static ImmutableArray<IdleActivityInfo> Subtract(this IdleActivityInfo item, DateRange range)
        {
            return IDateRangeUtils.Subtract(item, range);
        }

        public static ImmutableArray<IdleActivityInfo> Subtract(this ImmutableArray<IdleActivityInfo> items, DateRange range)
        {
            List<IdleActivityInfo> list = new List<IdleActivityInfo>();

            foreach (var item in items)
            {
                var remaining = item.Subtract(range);
                list.AddRange(remaining);
            }
            return list.ToImmutableArray();
        }

        public static ImmutableArray<IdleActivityInfo> Intersect(this ImmutableArray<IdleActivityInfo> items, DateRange range)
        {
            List<IdleActivityInfo> list = new List<IdleActivityInfo>();

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

                //trim all other cases
                DateTime intersectStart = Max(item.Start, range.Start);
                DateTime intersectStop = Min(item.Stop, range.Stop);

                list.Add(new IdleActivityInfo
                {
                    Type = item.Type,
                    Start = intersectStart,
                    Stop = intersectStop
                });
            }

            return list.ToImmutableArray();
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
