using System;
using System.Reactive.Linq;
using System.Threading;
using Noggog;
using Noggog.Reactive;
using Serilog;

namespace Synthesis.Bethesda.Execution.DotNet
{
    public interface IInstalledSdkProvider
    {
        IObservable<DotNetVersion> DotNetSdkInstalled { get; }
    }
    
    public class InstalledSdkProvider : IInstalledSdkProvider
    {
        public IObservable<DotNetVersion> DotNetSdkInstalled { get; }
        
        public InstalledSdkProvider(
            ISchedulerProvider schedulerProvider,
            IQueryInstalledSdk query, 
            ILogger logger)
        {
            var dotNet = Observable.Interval(TimeSpan.FromSeconds(10), schedulerProvider.TaskPool)
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
                .Do(x => logger.Information("DotNet SDK: {Version}", x))
                .Replay(1)
                .RefCount();
        }
    }
}