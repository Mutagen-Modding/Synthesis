using Noggog;
using Synthesis.Bethesda.Execution.Patchers.Git.Services.Instantiation;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Git;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.TopLevel;
using Synthesis.Bethesda.GUI.ViewModels.Profiles.Plugins;

namespace Synthesis.Bethesda.GUI.ViewModels.Profiles.PatcherInstantiation;

public interface IGitSettingsInitializer
{
    GithubPatcherSettings Get(GithubPatcherSettings? settings);
}

public class GitSettingsInitializer : IGitSettingsInitializer
{
    private readonly IProfilePatcherEnumerable _patchersList;
    private readonly GitIdAllocator _gitIdAllocator;

    public GitSettingsInitializer(
        IProfilePatcherEnumerable patchersList,
        GitIdAllocator gitIdAllocator)
    {
        _patchersList = patchersList;
        _gitIdAllocator = gitIdAllocator;
    }
        
    public GithubPatcherSettings Get(GithubPatcherSettings? settings)
    {
        settings ??= new GithubPatcherSettings();
        settings.ID = string.IsNullOrWhiteSpace(settings.ID) ? GetNewId() : settings.ID;
        return settings;
    }
        
    private string GetNewId()
    {
        var set = _patchersList.Patchers.WhereCastable<PatcherVm, GitPatcherVm>()
            .Select(x => x.ID)
            .ToHashSet();

        return _gitIdAllocator.GetNewId(set);
    }
}