using System.Diagnostics.CodeAnalysis;

namespace Synthesis.Bethesda.Execution.Settings;

public enum PatcherNugetVersioningEnum
{
    Profile,
    Latest,
    Match,
    Manual,
}

[ExcludeFromCodeCoverage]
public static class PatcherNugetVersioningEnumExt
{
    public static NugetVersioningEnum ToNugetVersioningEnum(this PatcherNugetVersioningEnum e)
    {
        return e switch
        {
            PatcherNugetVersioningEnum.Latest => NugetVersioningEnum.Latest,
            PatcherNugetVersioningEnum.Manual => NugetVersioningEnum.Manual,
            PatcherNugetVersioningEnum.Match => NugetVersioningEnum.Match,
            _ => throw new ArgumentException(),
        };
    }
}