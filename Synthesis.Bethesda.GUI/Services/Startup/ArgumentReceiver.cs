using System.Reactive.Linq;
using Noggog.IO;

namespace Synthesis.Bethesda.GUI.Services.Startup;

public class ArgumentReceiver : IWatchSingleAppArguments
{
    private readonly SingletonApplicationEnforcer _singleApp;
    private readonly string[] _args;

    public ArgumentReceiver(SingletonApplicationEnforcer singleApp, string[] args)
    {
        _singleApp = singleApp;
        _args = args;
    }

    public IObservable<IReadOnlyList<string>> WatchArgs()
    {
        return _singleApp.WatchArgs()
            .StartWith(_args);
    }
}