using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Synthesis.Bethesda.DTO;

public enum PreferredAutoVersioning
{
    /// <summary>
    /// Will perfer latest tag, if any tags exist
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
    public string? Nickname { get; set; }
    public VisibilityOptions Visibility { get; set; } = VisibilityOptions.Visible;
    public string? OneLineDescription { get; set; }
    public string? LongDescription { get; set; }
    public PreferredAutoVersioning PreferredAutoVersioning { get; set; }
    public string[] RequiredMods { get; set; } = Array.Empty<string>();
}