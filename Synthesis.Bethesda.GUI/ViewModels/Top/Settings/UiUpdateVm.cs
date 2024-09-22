using System.Reactive.Linq;
using System.Windows.Input;
using Mutagen.Bethesda.Synthesis.Versioning;
using Noggog;
using Noggog.WPF;
using NuGet.Versioning;
using ReactiveUI;
using Serilog;
using Synthesis.Bethesda.GUI.Services.Main;
using Synthesis.Bethesda.GUI.Services.Versioning;

namespace Synthesis.Bethesda.GUI.ViewModels.Top.Settings;

public class UiUpdateVm : ViewModel
{
    private readonly SemanticVersionParsing _semanticVersionParsing;
    public string SynthesisVersion { get; }

    private readonly ObservableAsPropertyHelper<string?> _newestSynthesisVersion;
    public string? NewestSynthesisVersion => _newestSynthesisVersion.Value;

    public string HelpText { get; } = 
        "This is the version of the UI executable itself.\nIt is generally recommended to update to the " +
        "newest UI when one is available.";

    public string WikiPage { get; } = $"https://github.com/Mutagen-Modding/Synthesis/wiki/Updating-UI";

    public string ReleasePage { get; } = $"https://github.com/Mutagen-Modding/Synthesis/releases";

    public ICommand GoToWikiCommand { get; }

    public ICommand GoToUpdatePageCommand { get; }

    private readonly ObservableAsPropertyHelper<bool> _hasUpdate;
    public bool HasUpdate => _hasUpdate.Value;
    
    public UiUpdateVm(
        ILogger logger,
        INavigateTo navigateTo,
        INewestLibraryVersionsVm libVersionsVm,
        IProvideCurrentVersions currentVersions,
        SemanticVersionParsing semanticVersionParsing)
    {
        _semanticVersionParsing = semanticVersionParsing;
        SynthesisVersion = currentVersions.SynthesisVersion;
        SemanticVersion? curVersion;
        try
        {
            if (!_semanticVersionParsing.TryTrimAndParse(SynthesisVersion, out curVersion))
            {
                curVersion = null;
                logger.Error("Error getting current UI semantic version: {String}", SynthesisVersion);
            }
        }
        catch (Exception e)
        {
            logger.Error(e, "Error getting current UI semantic version");
            curVersion = null;
        }
        _newestSynthesisVersion = libVersionsVm.WhenAnyValue(x => x.Versions)
            .Select(x =>
            {
                if (curVersion == null || !curVersion.IsPrerelease)
                {
                    return x.Normal.Synthesis;
                }

                return x.Prerelease.Synthesis;
            })
            .ToGuiProperty(this, nameof(NewestSynthesisVersion), default, deferSubscription: true);

        GoToWikiCommand = ReactiveCommand.Create(() => navigateTo.Navigate(WikiPage));

        GoToUpdatePageCommand = ReactiveCommand.Create(() => navigateTo.Navigate(ReleasePage));

        _hasUpdate = this.WhenAnyValue(x => x.NewestSynthesisVersion)
            .Select(x =>
            {
                if (curVersion == null) return false;
                if (x.IsNullOrWhitespace()) return false;
                try
                {
                    logger.Information("Checking if there is a UI update. {Current} -> {Newest}", curVersion, x);

                    if (!_semanticVersionParsing.TryTrimAndParse(x, out var newestVersion))
                    {
                        logger.Information("Failed to parse newest version");
                        return false;
                    }
                    
                    return newestVersion > curVersion;
                }
                catch (Exception e)
                {
                    logger.Error(e, "Error comparing if UI had new version");
                    return false;
                }
            })
            .ToGuiProperty(this, nameof(HasUpdate), deferSubscription: true);
    }
}