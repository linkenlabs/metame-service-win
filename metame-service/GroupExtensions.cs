using MetaMe.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaMe.WindowsClient
{
    static class GroupExtensions
    {
        public static ImmutableArray<Group> RemoveApp(this ImmutableArray<Group> items, string name)
        {
            var newSet = items.Select(item =>
            {
                return new Group
                {
                    Name = item.Name,
                    Items = item.Items.Where(i => i != name).ToArray()
                };
            }).ToImmutableArray();

            return newSet;
        }
    }
}
