namespace Synthesis.Bethesda.GUI.Settings;

public interface ISynthesisGuiSettings
{
    bool ShowHelp { get; set; }
    bool OpenIdeAfterCreating { get; set; }
    IDE Ide { get; set; }
    string MainRepositoryFolder { get; set; }
    string SelectedProfile { get; set; }
    BrowserSettings BrowserSettings { get; set; }
    bool SpecifyTargetFramework { get; set; }
    
}

public record SynthesisGuiSettings : ISynthesisGuiSettings
{
    public int Version => 2; 
    public bool ShowHelp { get; set; } = true;
    public bool OpenIdeAfterCreating { get; set; } = true;
    public IDE Ide { get; set; } = IDE.SystemDefault;
    public string MainRepositoryFolder { get; set; } = string.Empty;
    public string SelectedProfile { get; set; } = string.Empty;
    public BrowserSettings BrowserSettings { get; set; } = new(ShowUnlisted: false, ShowInstalled: true);
    public bool SpecifyTargetFramework { get; set; } = true;
    public string? TargetRuntime => SpecifyTargetFramework ? "win-x64" : null;
}

public record BrowserSettings(bool ShowUnlisted, bool ShowInstalled);