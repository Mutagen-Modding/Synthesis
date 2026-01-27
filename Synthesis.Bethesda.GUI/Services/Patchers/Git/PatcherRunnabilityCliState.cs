using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using DynamicData;
using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.WPF.Plugins.Order;
using Noggog;
using ReactiveUI;
using Serilog;
using Synthesis.Bethesda.Execution.PatcherCommands;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.GUI.ViewModels.Groups;
using Synthesis.Bethesda.GUI.ViewModels.Profiles.Plugins;

namespace Synthesis.Bethesda.GUI.Services.Patchers.Git;

public interface IPatcherRunnabilityCliState
{
    IObservable<ConfigurationState<RunnerRepoInfo>> Runnable { get; }
    void CheckAgain();
}

public class PatcherRunnabilityCliState : IPatcherRunnabilityCliState
{
    private readonly Subject<Unit> _checkAgain = new();
    public IObservable<ConfigurationState<RunnerRepoInfo>> Runnable { get; }
    
    public void CheckAgain() => _checkAgain.OnNext(Unit.Default);

    public PatcherRunnabilityCliState(
        ICompilationProvider compilationProvider,
        IProfileOverridesVm overrides,
        IProfileLoadOrder loadOrder,
        IExecuteRunnabilityCheck checkRunnability,
        ITemporaryLoadOrderProvider temporaryLoadOrderProvider,
        IModKeyProvider modKeyProvider,
        ILogger logger)
    {
        Runnable = Observable.CombineLatest(
                compilationProvider.State,
                overrides.WhenAnyValue(x => x.DataFolderResult.Value),
                loadOrder.LoadOrder.Connect()
                    .QueryWhenChanged()
                    .StartWith(Array.Empty<ReadOnlyModListingVM>()),
                (comp, data, loadOrder) => (comp, data, loadOrder))
            // ToDo
            // Replace with RepublishLatestOnSignal
            .Select(x =>
            {
                return _checkAgain.Select(_ => x)
                    .StartWith(x);
            })
            .Switch()
            .Select(i =>
            {
                return Observable.Create<ConfigurationState<RunnerRepoInfo>>(async (observer, cancel) =>
                {
                    if (i.comp.RunnableState.Failed)
                    {
                        observer.OnNext(i.comp.BubbleError<RunnerRepoInfo>());
                        return;
                    }

                    logger.Information("Checking runnability");
                    // Return early with the values, but mark not complete
                    observer.OnNext(new ConfigurationState<RunnerRepoInfo>(i.comp.Item.RunnerRepoInfo)
                    {
                        IsHaltingError = false,
                        RunnableState = ErrorResponse.Fail("Checking runnability")
                    });

                    try
                    {
                        var modKey = modKeyProvider.ModKey;
                        if (modKey == null)
                        {
                            logger.Information($"Checking runnability failed: No known ModKey");
                            observer.OnNext(new ConfigurationState<RunnerRepoInfo>(i.comp.Item.RunnerRepoInfo)
                            {
                                IsHaltingError = true,
                                RunnableState = ErrorResponse.Fail("No known ModKey")
                            });
                            return;
                        }

                        if (i.comp.Item.Meta.ExecutablePath == null)
                        {
                            logger.Error("ExecutablePath is null in compilation meta");
                            observer.OnNext(new ConfigurationState<RunnerRepoInfo>(i.comp.Item.RunnerRepoInfo)
                            {
                                IsHaltingError = true,
                                RunnableState = ErrorResponse.Fail("ExecutablePath is null in compilation meta. This likely indicates a compilation error.")
                            });
                            return;
                        }

                        using var tmpLoadOrder = temporaryLoadOrderProvider.Get(
                            i.loadOrder.Select<ReadOnlyModListingVM, IModListingGetter>(lvm => lvm));
                        var runnability = await checkRunnability.Check(
                            executablePath: i.comp.Item.Meta.ExecutablePath,
                            cancel: cancel,
                            modKey: modKey.Value,
                            buildMetaPath: i.comp.Item.RunnerRepoInfo.MetaPath,
                            loadOrderPath: tmpLoadOrder.File).ConfigureAwait(false);
                        if (runnability.Failed)
                        {
                            logger.Information($"Checking runnability failed: {runnability.Reason}");
                            observer.OnNext(runnability.BubbleFailure<RunnerRepoInfo>());
                            return;
                        }

                        // Return things again, without error
                        logger.Information("Checking runnability succeeded");
                        observer.OnNext(new ConfigurationState<RunnerRepoInfo>(i.comp.Item.RunnerRepoInfo)
                        {
                            IsHaltingError = false,
                            RunnableState = GetResponse<RunnerRepoInfo>.Succeed(i.comp.Item.RunnerRepoInfo)
                        });
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, "Error checking runnability on runner repository");
                        // Preserve the exception for error classification
                        observer.OnNext(new ConfigurationState<RunnerRepoInfo>(i.comp.Item.RunnerRepoInfo)
                        {
                            IsHaltingError = true,
                            RunnableState = GetResponse<RunnerRepoInfo>.Fail(default!, ex)
                        });
                    }

                    observer.OnCompleted();
                });
            })
            .Switch()
            .Replay(1)
            .RefCount();
    }
}