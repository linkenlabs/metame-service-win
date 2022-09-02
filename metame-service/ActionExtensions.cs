using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MetaMe.WindowsClient
{
    static class ActionExtensions
    {
        //From https://stackoverflow.com/a/29491927
        public static Action Debounce(this Action action, int milliseconds = 1000)
        {
            var last = 0;
            return () =>
            {
                var current = Interlocked.Increment(ref last);

                Task.Delay(milliseconds).ContinueWith(task =>
                {
                    if (current == last)
                    {
                        action();
                    }
                    task.Dispose();
                });
            };
        }
    }
}
