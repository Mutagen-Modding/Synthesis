using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Environments.DI;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Analysis;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Plugins.Records;
using Noggog;
using Noggog.Reactive;
using Noggog.UI;
using Noggog.WPF;
using ReactiveUI;
using Serilog;
using Synthesis.Bethesda.GUI.Services;

namespace Synthesis.Bethesda.GUI.ViewModels.Profiles.Plugins;

public interface IProfileSimpleLinkCacheVm
{
    ILinkCache? SimpleLinkCache { get; }
}

public interface IProfileGroupModKeyProvider
{
    IObservable<IReadOnlySet<ModKey>> GroupModKeys { get; }
}

public class ProfileSimpleLinkCacheVm : ViewModel, IProfileSimpleLinkCacheVm
{
    private readonly ObservableAsPropertyHelper<ILinkCache?> _simpleLinkCache;
    public ILinkCache? SimpleLinkCache => _simpleLinkCache.Value;

    public ProfileSimpleLinkCacheVm(
        ILogger logger,
        IGameReleaseContext gameReleaseContext,
        IProfileLoadOrder loadOrder,
        IProfileOverridesVm overrides,
        IProfileGroupModKeyProvider groupModKeyProvider,
        ISchedulerProvider schedulerProvider)
    {
        _simpleLinkCache = Observable.CombineLatest(
                overrides.WhenAnyValue(x => x.DataFolderResult.Value),
                loadOrder.LoadOrder.Connect()
                    .QueryWhenChanged()
                    .Select(q => q.Where(x => x.Enabled).Select(x => x.ModKey).ToArray())
                    .StartWithEmpty(),
                groupModKeyProvider.GroupModKeys,
                (dataFolder, loadOrder, groupKeys) => (dataFolder, loadOrder, groupKeys))
            .Throttle(TimeSpan.FromMilliseconds(100), schedulerProvider.TaskPool)
            .Select(x =>
            {
                // Exclude Synthesis output mods (and their split siblings like _1, _2, etc.)
                // from the link cache import. Importing them would hold file handles via
                // binary overlays, blocking MoveFinalResults from overwriting them on
                // subsequent runs.
                var filteredLoadOrder = x.loadOrder
                    .Where(mk => !x.groupKeys.Contains(mk)
                                 && !x.groupKeys.Any(gk => MultiModFileAnalysis.IsSplitModSibling(mk, gk)))
                    .ToArray();

                return Observable.Create<(ILinkCache? Cache, IDisposable Disposable)>(obs =>
                {
                    try
                    {
                        var importedLoadOrder = Mutagen.Bethesda.Plugins.Order.LoadOrder.Import(
                            x.dataFolder,
                            filteredLoadOrder,
                            gameReleaseContext.Release,
                            factory: (modPath) => ModInstantiator.ImportGetter(modPath, gameReleaseContext.Release));
                        obs.OnNext(
                            (importedLoadOrder.ToUntypedImmutableLinkCache(LinkCachePreferences.OnlyIdentifiers()),
                                importedLoadOrder));
                        // Don't call OnCompleted - it triggers immediate subscription disposal,
                        // which would dispose the load order before DisposePrevious can manage it.
                        // Instead, return it as the disposable so Switch() disposes it
                        // when a new inner observable arrives or when the chain tears down.
                        return importedLoadOrder;
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, "Error creating simple link cache for GUI lookups");
                        obs.OnNext((default, Disposable.Empty));
                        obs.OnCompleted();
                    }

                    return Disposable.Empty;
                });
            })
            .Switch()
            .DisposePrevious(x => x.Disposable)
            .Select(x => x.Cache)
            .ToGuiProperty(this, nameof(SimpleLinkCache), default(ILinkCache?), schedulerProvider.MainThread, deferSubscription: true);
    }
}