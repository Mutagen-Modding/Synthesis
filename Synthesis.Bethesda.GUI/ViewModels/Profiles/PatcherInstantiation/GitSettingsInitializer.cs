using System;
using System.IO;
using Noggog;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.GUI.ViewModels.Patchers;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.Git;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.TopLevel;
using Synthesis.Bethesda.GUI.ViewModels.Profiles.Plugins;

namespace Synthesis.Bethesda.GUI.ViewModels.Profiles.PatcherInstantiation
{
    public interface IGitSettingsInitializer
    {
        GithubPatcherSettings Get(GithubPatcherSettings? settings);
    }

    public class GitSettingsInitializer : IGitSettingsInitializer
    {
        private readonly IProfilePatchersList _patchersList;

        public GitSettingsInitializer(
            IProfilePatchersList patchersList)
        {
            _patchersList = patchersList;
        }
        
        public GithubPatcherSettings Get(GithubPatcherSettings? settings)
        {
            settings ??= new GithubPatcherSettings();
            settings.ID = string.IsNullOrWhiteSpace(settings.ID) ? GetNewId() : settings.ID;
            return settings;
        }
        
        private string GetNewId()
        {
            bool IsValid(string id)
            {
                foreach (var patcher in _patchersList.Patchers.Items.WhereCastable<PatcherVm, GitPatcherVm>())
                {
                    if (patcher.ID == id)
                    {
                        return false;
                    }
                }
                return true;
            }

            for (int i = 0; i < 15; i++)
            {
                var attempt = Path.GetRandomFileName();
                if (IsValid(attempt))
                {
                    return attempt;
                }
            }

            throw new ArgumentException("Could not allocate a new profile");
        }
    }
}