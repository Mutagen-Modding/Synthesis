using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using Mutagen.Bethesda.Plugins.Order.DI;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using Serilog;
using Synthesis.Bethesda.GUI.ViewModels.Profiles.Plugins;

namespace Synthesis.Bethesda.GUI.ViewModels.EnvironmentErrors;

public class PluginsTxtMissingVm : ViewModel, IEnvironmentErrorVm
{
    private readonly ObservableAsPropertyHelper<bool> _inError;
    public bool InError => _inError.Value;

    public string? ErrorString { get; }

    public FilePath PluginFilePath { get; }

    public PluginsTxtMissingVm(
        ILogger logger,
        IPluginListingsPathContext listingsPathProvider)
    {
        PluginFilePath = listingsPathProvider.Path;
        
        _inError = Noggog.ObservableExt.WatchFile(PluginFilePath)
            .StartWith(Unit.Default)
            .Select(_ =>
            {
                try
                {
                    return !File.Exists(PluginFilePath);
                }
                catch (Exception e)
                {
                    logger.Error(e, "Error checking for plugin.txt existence");
                    return true;
                }
            })
            .ToGuiProperty(this, nameof(InError), deferSubscription: true);

        ErrorString = $"Could not find plugin file to read the load order from. \n\n" +
                      $"- Run your game once, to generate the file\n" +
                      $"- If using MO2, make sure Synthesis is being started through MO2";
    }
}