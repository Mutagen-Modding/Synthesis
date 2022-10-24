using System.Reactive.Linq;
using DynamicData;
using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.WPF.Plugins.Order;
using Noggog;
using ReactiveUI;
using Serilog;
using Synthesis.Bethesda.Execution.PatcherCommands;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.GUI.ViewModels.Groups;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.TopLevel;
using Synthesis.Bethesda.GUI.ViewModels.Profiles.Plugins;

namespace Synthesis.Bethesda.GUI.Services.Patchers.Git;

public interface IPatcherRunnabilityCliState
{
    IObservable<ConfigurationState<RunnerRepoInfo>> Runnable { get; }
}

public class PatcherRunnabilityCliState : IPatcherRunnabilityCliState
{
    public IObservable<ConfigurationState<RunnerRepoInfo>> Runnable { get; }

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
            .Select(i =>
            {
                return Observable.Create<ConfigurationState<RunnerRepoInfo>>(async (observer, cancel) =>
                {
                    if (i.comp.RunnableState.Failed)
                    {
                        observer.OnNext(i.comp);
                        return;
                    }

                    logger.Information("Checking runnability");
                    // Return early with the values, but mark not complete
                    observer.OnNext(new ConfigurationState<RunnerRepoInfo>(i.comp.Item)
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
                            observer.OnNext(new ConfigurationState<RunnerRepoInfo>(i.comp.Item)
                            {
                                IsHaltingError = true,
                                RunnableState = ErrorResponse.Fail("No known ModKey")
                            });
                            return;
                        }
                        
                        using var tmpLoadOrder = temporaryLoadOrderProvider.Get(
                            i.loadOrder.Select<ReadOnlyModListingVM, IModListingGetter>(lvm => lvm));
                        var runnability = await checkRunnability.Check(
                            path: i.comp.Item.Project.ProjPath,
                            directExe: false,
                            cancel: cancel,
                            modKey: modKey.Value,
                            buildMetaPath: i.comp.Item.MetaPath,
                            loadOrderPath: tmpLoadOrder.File).ConfigureAwait(false);
                        if (runnability.Failed)
                        {
                            logger.Information($"Checking runnability failed: {runnability.Reason}");
                            observer.OnNext(runnability.BubbleFailure<RunnerRepoInfo>());
                            return;
                        }

                        // Return things again, without error
                        logger.Information("Checking runnability succeeded");
                        observer.OnNext(i.comp);
                    }
                    catch (Exception ex)
                    {
                        var str = $"Error checking runnability on runner repository: {ex}";
                        logger.Error(str);
                        observer.OnNext(ErrorResponse.Fail(str).BubbleFailure<RunnerRepoInfo>());
                    }

                    observer.OnCompleted();
                });
            })
            .Switch()
            .Replay(1)
            .RefCount();
    }
}