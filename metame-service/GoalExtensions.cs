using MetaMe.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaMe.WindowsClient
{
    static class GoalExtensions
    {
        public static bool IsOldFormat(this ImmutableArray<Goal> goals)
        {
            if (goals.Length != 1)
            {
                return false;
            }

            var goal = goals.First();
            bool isNewFormat = goal.Key == "Productive" && goal.Name == "Productive";
            return !isNewFormat;

        }

    }
}
