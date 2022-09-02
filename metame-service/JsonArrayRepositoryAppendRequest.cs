using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MetaMe.WindowsClient
{
    class JsonArrayRepositoryAppendRequest<T>
    {
        public TaskCompletionSource<bool> TaskCompletionSource { get; private set; }
        public ImmutableArray<T> Buffer { get; private set; }

        public JsonArrayRepositoryAppendRequest(TaskCompletionSource<bool> source, ImmutableArray<T> buffer)
        {
            TaskCompletionSource = source;
            Buffer = buffer;
        }
    }
}
