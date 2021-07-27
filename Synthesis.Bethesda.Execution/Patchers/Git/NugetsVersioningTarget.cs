using Synthesis.Bethesda.Execution.Settings;
using Serilog;

namespace Synthesis.Bethesda.Execution.Patchers.Git
{
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
    }
}