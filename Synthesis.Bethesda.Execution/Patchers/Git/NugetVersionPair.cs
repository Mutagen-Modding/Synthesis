using Serilog;

namespace Synthesis.Bethesda.Execution.Patchers.Git
{
    public record NugetVersionPair(string? Mutagen, string? Synthesis);
}