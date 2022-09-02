using MetaMe.Core;
using System.Collections.Immutable;

namespace MetaMe.WindowsClient
{
    class AppActivityFactGeneratorStateChangedEventArgs
    {
        public AppActivityFactGeneratorState State { get; set; }
        public ImmutableArray<AppActivityFact> Facts { get; set; }
    }
}
