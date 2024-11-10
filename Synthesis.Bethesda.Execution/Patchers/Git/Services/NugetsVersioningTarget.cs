using Serilog;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.Execution.Versioning;

namespace Synthesis.Bethesda.Execution.Patchers.Git.Services;

public record NugetVersioningTarget(string? Version, NugetVersioningEnum Versioning)
{
    public string? ReturnIfMatch(string? ifMatch)
    {
        return Versioning == NugetVersioningEnum.Match ? ifMatch : Version;
    }
}
    
public record NugetsVersioningTarget(
    NugetVersioningTarget Mutagen,
    NugetVersioningTarget Synthesis)
{
    public void Log(ILogger logger)
    {
        logger.Information("Mutagen Nuget: {Versioning} {Version}", Mutagen.Versioning, Mutagen.Version);
        logger.Information("Synthesis Nuget: {Versioning} {Version}", Synthesis.Versioning, Synthesis.Version);
    }
        
    public NugetVersionPair ReturnIfMatch(NugetVersionPair pair)
    {
        return new NugetVersionPair(
            Mutagen: Mutagen.Versioning == NugetVersioningEnum.Match ? pair.Mutagen : Mutagen.Version,
            Synthesis: Synthesis.Versioning == NugetVersioningEnum.Match ? pair.Synthesis : Synthesis.Version);
    }
}