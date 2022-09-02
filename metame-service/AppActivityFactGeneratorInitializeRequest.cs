using System;
using System.Threading.Tasks;

namespace MetaMe.WindowsClient
{
    class AppActivityFactGeneratorInitializeRequest
    {
        public DateTime Pointer { get; set; }
        public TaskCompletionSource<bool> TaskCompletionSource { get; set; }
    }
}
