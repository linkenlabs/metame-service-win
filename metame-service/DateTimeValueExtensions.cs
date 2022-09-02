using MetaMe.WindowsClient.controllers;
using System.Collections.Immutable;
using System.Linq;

namespace MetaMe.WindowsClient
{
    static class DateTimeValueExtensions
    {
        //copy source data into dest
        public static ImmutableArray<DateTimeValue> UpdateWith(this ImmutableArray<DateTimeValue> destination, ImmutableArray<DateTimeValue> source)
        {
            var updated = destination.Select(datum =>
            {
                var updatedDatum = source.FirstOrDefault(d => d.DateTime.ToUniversalTime() == datum.DateTime.ToUniversalTime());
                return new DateTimeValue
                {
                    DateTime = datum.DateTime,
                    Value = updatedDatum != null ? updatedDatum.Value : datum.Value
                };
            }).ToImmutableArray();
            return updated;

        }
    }
}
