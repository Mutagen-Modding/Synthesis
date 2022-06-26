using Mutagen.Bethesda;

namespace Synthesis.Bethesda.DTO;

public class PatcherListing
{
    public PatcherCustomization? Customization { get; set; }
    
    public string ProjectPath { get; set; } = string.Empty;
    
    /// <summary>
    /// What libraries are imported into the patcher (Mutagen.Bethesda.Skyrim, for example)
    /// </summary>
    public GameCategory[] IncludedLibraries { get; set; } = Array.Empty<GameCategory>();
}