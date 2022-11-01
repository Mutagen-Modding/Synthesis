using System.IO;
using System.Reactive;
using System.Reactive.Linq;
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

    private readonly ObservableAsPropertyHelper<FilePath> _pluginFilePath;
    public FilePath PluginFilePath => _pluginFilePath.Value;

    public PluginsTxtMissingVm(
        ILogger logger,
        IProfileOverridesVm profile)
    {
        _pluginFilePath = profile.WhenAnyValue(x => x.PluginListingsPath)
            .ToGuiProperty(this, nameof(PluginFilePath));
        
        _inError = this.WhenAnyValue(x => x.PluginFilePath)
            .Select(path =>
            {
                return Noggog.ObservableExt.WatchFile(path)
                    .StartWith(Unit.Default)
                    .Select(_ =>
                    {
                        try
                        {
                            return !File.Exists(path);
                        }
                        catch (Exception e)
                        {
                            logger.Error(e, "Error checking for plugin.txt existence");
                            return true;
                        }
                    });
            })
            .Switch()
            .ToGuiProperty(this, nameof(InError), deferSubscription: true);

        ErrorString = $"Could not find plugin file to read the load order from. \n\n" +
                      $"- Run your game once, to generate the file\n" +
                      $"- If using MO2, make sure Synthesis is being started through MO2";
    }
}