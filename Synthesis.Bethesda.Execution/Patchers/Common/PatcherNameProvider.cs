using System.Diagnostics.CodeAnalysis;

namespace Synthesis.Bethesda.Execution.Patchers.Common
{
    public interface IPatcherNameProvider
    {
        string Name { get; }
    }

    [ExcludeFromCodeCoverage]
    public class PatcherNameInjection : IPatcherNameProvider
    {
        public string Name { get; init; } = string.Empty;
    }
}