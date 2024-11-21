using System.Diagnostics.CodeAnalysis;

namespace Synthesis.Bethesda.Execution.Patchers.Git.Services.PrepareDriver;

[ExcludeFromCodeCoverage]
public record DriverTag(int Index, string Name, string Sha);