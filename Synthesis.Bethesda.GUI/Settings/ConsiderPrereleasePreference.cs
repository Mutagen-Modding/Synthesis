using System;
using Noggog.WPF;
using Synthesis.Bethesda.Execution.Versioning;

namespace Synthesis.Bethesda.GUI.Settings
{
    public class ConsiderPrereleasePreference : IConsiderPrereleasePreference
    {
        public IObservable<bool> ConsiderPrereleases { get; }

        public ConsiderPrereleasePreference(ISelectedProfileControllerVm selectedProfile)
        {
            ConsiderPrereleases = selectedProfile.WhenAnyFallback(x => x.SelectedProfile!.ConsiderPrereleaseNugets);
        }
    }
}