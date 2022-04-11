using System;

namespace Synthesis.Bethesda.GUI.Settings;

public interface ISynthesisGuiSettings
{
    bool ShowHelp { get; set; }
    bool OpenIdeAfterCreating { get; set; }
    IDE Ide { get; set; }
    string MainRepositoryFolder { get; set; }
    string SelectedProfile { get; set; }
    string WorkingDirectory { get; set; }
    double BuildCorePercentage { get; set; }
    string DotNetPathOverride { get; set; }
    bool ShowAllGitPatchersInBrowser { get; set; }
}

public record SynthesisGuiSettings : ISynthesisGuiSettings
{
    public bool ShowHelp { get; set; } = true;
    public bool OpenIdeAfterCreating { get; set; } = true;
    public IDE Ide { get; set; } = IDE.SystemDefault;
    public string MainRepositoryFolder { get; set; } = string.Empty;
    public string SelectedProfile { get; set; } = string.Empty;
    public string WorkingDirectory { get; set; } = string.Empty;
    public double BuildCorePercentage { get; set; } = 0.5d;
    public string DotNetPathOverride { get; set; } = string.Empty;
    public bool ShowAllGitPatchersInBrowser { get; set; }
}