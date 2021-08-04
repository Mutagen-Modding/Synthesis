using System.Diagnostics.CodeAnalysis;

namespace Synthesis.Bethesda.Execution.DotNet.Dto
{
    [ExcludeFromCodeCoverage]
    public record NugetListingQuery(string Package, string Requested, string Resolved, string Latest);
}