using Path = System.IO.Path;
using System.IO.Abstractions;
using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;
using Synthesis.Bethesda.DTO;
using Synthesis.Bethesda.Execution.Settings.Json;

namespace Synthesis.Bethesda.GUI.Services.Patchers.Git;

public interface IPatcherConfigurationWatcher
{
    IObservable<PatcherCustomization?> Customization { get; }
}

public class PatcherConfigurationWatcher : IPatcherConfigurationWatcher
{
    public IObservable<PatcherCustomization?> Customization { get; }

    public PatcherConfigurationWatcher(
        IFileSystem fileSystem,
        IPatcherCustomizationImporter customizationImporter,
        IRunnableStateProvider runnableStateProvider)
    {
        Customization = runnableStateProvider.WhenAnyValue(x => x.State)
            .Select(x =>
            {
                if (x.RunnableState.Failed) return Observable.Return(default(PatcherCustomization?));
                var confPath = Path.Combine(Path.GetDirectoryName(x.Item.Project.ProjPath)!, Constants.MetaFileName);
                return Noggog.ObservableExt.WatchFile(confPath)
                    .StartWith(Unit.Default)
                    .Select(_ =>
                    {
                        try
                        {
                            if (!fileSystem.File.Exists(confPath)) return default;
                            return customizationImporter.Import(confPath);
                        }
                        catch (Exception)
                        {
                            return default(PatcherCustomization?);
                        }
                    });
            })
            .Switch()
            .Replay(1)
            .RefCount();
    }
}