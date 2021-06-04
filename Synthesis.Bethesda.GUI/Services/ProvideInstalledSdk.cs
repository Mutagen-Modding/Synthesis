using System;
using System.Reactive.Linq;
using System.Threading;
using Noggog;
using ReactiveUI;
using Synthesis.Bethesda.Execution.DotNet;

namespace Synthesis.Bethesda.GUI
{
    public interface IProvideInstalledSdk
    {
        IObservable<DotNetVersion> DotNetSdkInstalled { get; }
    }
    
    public class ProvideInstalledSdk : IProvideInstalledSdk
    {
        public IObservable<DotNetVersion> DotNetSdkInstalled { get; }
        
        public ProvideInstalledSdk(IQueryInstalledSdk query)
        {
            var dotNet = Observable.Interval(TimeSpan.FromSeconds(10), RxApp.TaskpoolScheduler)
                .StartWith(0)
                .SelectTask(async i =>
                {
                    try
                    {
                        return await query.Query(CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        Log.Logger.Error(ex, $"Error retrieving dotnet SDK version");
                        return new DotNetVersion(string.Empty, false);
                    }
                });
            DotNetSdkInstalled = dotNet
                .Take(1)
                .Merge(dotNet
                    .FirstAsync(v => v != null))
                .DistinctUntilChanged()
                .Do(x => Log.Logger.Information($"dotnet SDK: {x}"))
                .Replay(1)
                .RefCount();
        }
    }
}