using System.Reactive.Linq;
using Noggog.IO;

namespace Synthesis.Bethesda.IntegrationTests.Components;

public class WatchSingleAppArgumentsStub : IWatchSingleAppArguments
{
    public IObservable<IReadOnlyList<string>> WatchArgs()
    {
        return Observable.Empty<IReadOnlyList<string>>();
    }
}