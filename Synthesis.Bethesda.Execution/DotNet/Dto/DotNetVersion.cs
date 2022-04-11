using System.Diagnostics.CodeAnalysis;

namespace Synthesis.Bethesda.Execution.DotNet.Dto;

[ExcludeFromCodeCoverage]
public record DotNetVersion(string Version, bool Acceptable);