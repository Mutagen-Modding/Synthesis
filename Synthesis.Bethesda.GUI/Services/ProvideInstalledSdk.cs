using System;
using System.Reactive.Linq;
using System.Threading;
using Noggog;
using ReactiveUI;
using Serilog;
using Synthesis.Bethesda.Execution.DotNet;

namespace Synthesis.Bethesda.GUI.Services
{
    public interface IProvideInstalledSdk
    {
        IObservable<DotNetVersion> DotNetSdkInstalled { get; }
    }
    
    public class ProvideInstalledSdk : IProvideInstalledSdk
    {
        public IObservable<DotNetVersion> DotNetSdkInstalled { get; }
        
        public ProvideInstalledSdk(IQueryInstalledSdk query, ILogger logger)
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
                        logger.Error(ex, $"Error retrieving dotnet SDK version");
                        return new DotNetVersion(string.Empty, false);
                    }
                });
            DotNetSdkInstalled = dotNet
                .Take(1)
                .Merge(dotNet
                    .FirstAsync(v => v != null))
                .DistinctUntilChanged()
                .Do(x => logger.Information("dotnet SDK: {Version}", x))
                .Replay(1)
                .RefCount();
        }
    }
}