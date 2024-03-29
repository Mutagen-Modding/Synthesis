using System.ComponentModel;
using Mutagen.Bethesda;

namespace Synthesis.Bethesda.DTO;

public enum PreferredAutoVersioning
{
    /// <summary>
    /// Will prefer latest tag, if any tags exist
    /// </summary>
    [Description("Default")]
    Default,

    /// <summary>
    /// Uses latest tag for preferred automatic versioning
    /// </summary>
    [Description("Latest Tag")]
    LatestTag,

    /// <summary>
    /// Uses latest default branch for preferred automatic versioning
    /// </summary>
    [Description("Latest Default Branch")]
    LatestDefaultBranch,
}

public enum VisibilityOptions
{
    /// <summary>
    /// Completely excludes the patcher from being registered to Synthesis.
    /// </summary>
    [Description("Exclude")]
    Exclude,

    /// <summary>
    /// Includes patcher into Synthesis, but hides it by default.
    /// </summary>
    [Description("Include But Hide")]
    IncludeButHide,

    /// <summary>
    /// Patcher will always be visible.
    /// </summary>
    [Description("Always Visible")]
    Visible
}

public record PatcherCustomization
{
    /// <summary>
    /// Nickname to use for display
    /// </summary>
    public string? Nickname { get; set; }
    
    /// <summary>
    /// Whether to include the patcher in the listing systems
    /// </summary>
    public VisibilityOptions Visibility { get; set; } = VisibilityOptions.Visible;
    
    /// <summary>
    /// Quick one-line summary description of the patcher
    /// </summary>
    public string? OneLineDescription { get; set; }
    
    /// <summary>
    /// Extensive multi-line description of the patcher
    /// </summary>
    public string? LongDescription { get; set; }
    
    /// <summary>
    /// What versioning pattern to prefer
    /// </summary>
    public PreferredAutoVersioning PreferredAutoVersioning { get; set; }
    
    /// <summary>
    /// Mods that must be on the user's load order for the patcher to be able to run
    /// </summary>
    public string[] RequiredMods { get; set; } = Array.Empty<string>();
    
    /// <summary>
    /// What Game Releases the patcher intends to support.  If left empty, the Mutagen libraries referenced by the
    /// project are used to determine viable targets.
    /// </summary>
    public GameRelease[] TargetedReleases { get; set; } = Array.Empty<GameRelease>();
    
    public virtual bool Equals(PatcherCustomization? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Nickname == other.Nickname 
               && Visibility == other.Visibility
               && OneLineDescription == other.OneLineDescription
               && LongDescription == other.LongDescription 
               && PreferredAutoVersioning == other.PreferredAutoVersioning 
               && RequiredMods.SequenceEqual(other.RequiredMods)
               && TargetedReleases.SequenceEqual(other.TargetedReleases);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            Nickname,
            (int)Visibility,
            OneLineDescription,
            LongDescription, 
            (int)PreferredAutoVersioning,
            RequiredMods,
            TargetedReleases);
    }
}