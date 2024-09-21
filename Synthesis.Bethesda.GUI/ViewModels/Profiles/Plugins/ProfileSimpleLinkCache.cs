using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Environments.DI;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Plugins.Records;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using Serilog;
using Synthesis.Bethesda.Execution.Profile;

namespace Synthesis.Bethesda.GUI.ViewModels.Profiles.Plugins;

public interface IProfileSimpleLinkCacheVm
{
    ILinkCache? SimpleLinkCache { get; }
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
        IProfileIdentifier ident)
    {
        _simpleLinkCache = Observable.CombineLatest(
                overrides.WhenAnyValue(x => x.DataFolderResult.Value),
                loadOrder.LoadOrder.Connect()
                    .QueryWhenChanged()
                    .Select(q => q.Where(x => x.Enabled).Select(x => x.ModKey).ToArray())
                    .StartWithEmpty(),
                (dataFolder, loadOrder) => (dataFolder, loadOrder))
            .Throttle(TimeSpan.FromMilliseconds(100), RxApp.TaskpoolScheduler)
            .Select(x =>
            {
                return Observable.Create<(ILinkCache? Cache, IDisposable Disposable)>(obs =>
                {
                    try
                    {
                        var loadOrder = Mutagen.Bethesda.Plugins.Order.LoadOrder.Import(
                            x.dataFolder,
                            x.loadOrder,
                            gameReleaseContext.Release,
                            factory: (modPath) => ModInstantiator.Importer(modPath, ident.Release));
                        obs.OnNext(
                            (loadOrder.ToUntypedImmutableLinkCache(LinkCachePreferences.OnlyIdentifiers()),
                                loadOrder));
                        obs.OnCompleted();
                        // ToDo
                        // Figure out why returning this is disposing too early.
                        // Gets disposed undesirably, which makes formlink pickers fail
                        // return loadOrder;
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
            .ToGuiProperty(this, nameof(SimpleLinkCache), default(ILinkCache?), deferSubscription: true);
    }
}