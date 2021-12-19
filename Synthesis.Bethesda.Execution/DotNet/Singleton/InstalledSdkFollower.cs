using System;
using System.Reactive.Linq;
using System.Threading;
using Noggog;
using Noggog.Reactive;
using Serilog;
using Synthesis.Bethesda.Execution.DotNet.Dto;

namespace Synthesis.Bethesda.Execution.DotNet
{
    public interface IInstalledSdkFollower
    {
        IObservable<DotNetVersion> DotNetSdkInstalled { get; }
    }
    
    public class InstalledSdkFollower : IInstalledSdkFollower
    {
        public static readonly TimeSpan RequeryTime = TimeSpan.FromSeconds(10);
        
        public IQueryInstalledSdk Query { get; }
        public ILogger Logger { get; }
        public IObservable<DotNetVersion> DotNetSdkInstalled { get; }
        
        public InstalledSdkFollower(
            ISchedulerProvider schedulerProvider,
            IQueryInstalledSdk query, 
            ILogger logger)
        {
            Query = query;
            Logger = logger;
            var dotNet = Observable.Interval(RequeryTime, schedulerProvider.TaskPool)
                .StartWith(0)
                .SelectTask(async _ =>
                {
                    try
                    {
                        return await query.Query(CancellationToken.None).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, "Error retrieving dotnet SDK version");
                        return new DotNetVersion(string.Empty, false);
                    }
                });
            DotNetSdkInstalled = dotNet
                .TakeUntil(x => x.Acceptable)
                .DistinctUntilChanged()
                .Do(x => logger.Information("DotNet SDK: {Version}", x))
                .Replay(1)
                .RefCount();
        }
    }
}