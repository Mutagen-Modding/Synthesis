using System.Diagnostics.CodeAnalysis;

namespace Synthesis.Bethesda.Execution.Versioning;

[ExcludeFromCodeCoverage]
public record NugetVersionOptions(
    NugetVersionPair Normal,
    NugetVersionPair Prerelease);