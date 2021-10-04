using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Plugins.Records;
using ReactiveUI;
using Serilog;
using Synthesis.Bethesda.Execution.Profile;

namespace Synthesis.Bethesda.GUI.ViewModels.Profiles.Plugins
{
    public interface IProfileSimpleLinkCache
    {
        IObservable<ILinkCache?> SimpleLinkCache { get; }
    }

    public class ProfileSimpleLinkCache : IProfileSimpleLinkCache
    {
        public IObservable<ILinkCache?> SimpleLinkCache { get; }

        public ProfileSimpleLinkCache(
            ILogger logger,
            IProfileLoadOrder loadOrder,
            IProfileDataFolderVm dataFolder,
            IProfileIdentifier ident)
        {
            SimpleLinkCache = Observable.CombineLatest(
                    dataFolder.WhenAnyValue(x => x.Path),
                    loadOrder.LoadOrder.Connect()
                        .QueryWhenChanged()
                        .Select(q => q.Where(x => x.Enabled).Select(x => x.ModKey).ToArray())
                        .StartWithEmpty(),
                    (dataFolder, loadOrder) => (dataFolder, loadOrder))
                .Throttle(TimeSpan.FromMilliseconds(100), RxApp.TaskpoolScheduler)
                .Select(x =>
                {
                    return Observable.Create<ILinkCache?>(obs =>
                    {
                        try
                        {
                            var loadOrder = Mutagen.Bethesda.Plugins.Order.LoadOrder.Import(
                                x.dataFolder,
                                x.loadOrder,
                                factory: (modPath) => ModInstantiator.Importer(modPath, ident.Release));
                            obs.OnNext(loadOrder.ToUntypedImmutableLinkCache(LinkCachePreferences.OnlyIdentifiers()));
                        }
                        catch (Exception ex)
                        {
                            logger.Error(ex, "Error creating simple link cache for GUI lookups");
                            obs.OnNext(null);
                        }
                        obs.OnCompleted();
                        return Disposable.Empty;
                    });
                })
                .Switch()
                .Replay(1)
                .RefCount();
        }
    }
}