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

namespace Synthesis.Bethesda.GUI.Temporary
{
    public class ProfileSimpleLinkCache
    {
        public IObservable<ILinkCache?> SimpleLinkCache { get; }

        public ProfileSimpleLinkCache(
            ILogger logger,
            ProfileLoadOrder loadOrder,
            ProfileDataFolder dataFolder,
            ProfileIdentifier ident)
        {
            SimpleLinkCache = Observable.CombineLatest(
                    dataFolder.WhenAnyValue(x => x.DataFolder),
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
                            logger.Error("Error creating simple link cache for GUI lookups", ex);
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