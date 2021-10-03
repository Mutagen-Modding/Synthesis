using System.Diagnostics.CodeAnalysis;

namespace Synthesis.Bethesda.Execution.Versioning
{
    [ExcludeFromCodeCoverage]
    public record NugetVersionPair(string? Mutagen, string? Synthesis);
}