using System.Reactive.Linq;
using DynamicData;
using Mutagen.Bethesda.Plugins;
using Noggog;
using Synthesis.Bethesda.GUI.ViewModels.Profiles.Plugins;

namespace Synthesis.Bethesda.GUI.Services.Patchers.Git;

public interface IMissingMods
{
    IObservable<IChangeSet<ModKey>> Missing { get; }
}

public class MissingMods : IMissingMods
{
    public IObservable<IChangeSet<ModKey>> Missing { get; }

    public MissingMods(
        IProfileLoadOrder loadOrder,
        IPatcherConfigurationWatcher configurationWatcher)
    {
        Missing = configurationWatcher.Customization
            .Select(conf =>
            {
                if (conf == null) return Enumerable.Empty<ModKey>();
                return conf.RequiredMods
                    .SelectWhere(x => TryGet<ModKey>.Create(ModKey.TryFromNameAndExtension(x, out var modKey), modKey));
            })
            .Select(x => x.AsObservableChangeSet())
            .Switch()
            .Except(loadOrder.LoadOrder.Connect()
                .Transform(x => x.ModKey))
            .RefCount();
    }
}