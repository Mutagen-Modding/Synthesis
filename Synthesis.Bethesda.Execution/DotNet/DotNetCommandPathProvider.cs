using System.Diagnostics.CodeAnalysis;

namespace Synthesis.Bethesda.Execution.DotNet
{
    public interface IDotNetCommandPathProvider
    {
        string Path { get; }
    }

    [ExcludeFromCodeCoverage]
    public class DotNetCommandPathProvider : IDotNetCommandPathProvider
    {
        public string Path => "dotnet";
    }
}