﻿using Noggog.WPF;
using Synthesis.Bethesda.Execution.Versioning;
using Synthesis.Bethesda.GUI.ViewModels.Top;

namespace Synthesis.Bethesda.GUI.Services.Profile.TopLevel;

public interface IConsiderPrereleasePreference
{
    IObservable<bool> ConsiderPrereleases { get; }
}

public class ConsiderPrereleasePreference : IConsiderPrereleasePreference
{
    public IObservable<bool> ConsiderPrereleases { get; }

    public ConsiderPrereleasePreference(ISelectedProfileControllerVm selectedProfile)
    {
        ConsiderPrereleases = selectedProfile.WhenAnyFallback(x => x.SelectedProfile!.ConsiderPrereleaseNugets);
    }
}