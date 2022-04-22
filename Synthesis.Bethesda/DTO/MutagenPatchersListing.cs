using System.Diagnostics.CodeAnalysis;

namespace Synthesis.Bethesda.DTO;

[ExcludeFromCodeCoverage]
public class MutagenPatchersListing
{
    public RepositoryListing[] Repositories { get; set; } = Array.Empty<RepositoryListing>();
}