using System.Diagnostics.CodeAnalysis;
using Serilog;

namespace Synthesis.Bethesda.Execution.Patchers.Git
{
    [ExcludeFromCodeCoverage]
    public record NugetVersionPair(string? Mutagen, string? Synthesis);
}