using System.Diagnostics.CodeAnalysis;

namespace Synthesis.Bethesda.Execution.DotNet
{
    [ExcludeFromCodeCoverage]
    public record DotNetVersion(string Version, bool Acceptable);
}