using System.Diagnostics.CodeAnalysis;

namespace Synthesis.Bethesda.Execution.Profile
{
    public interface IProfileNameProvider
    {
        string Name { get; }
    }

    [ExcludeFromCodeCoverage]
    public class ProfileNameInjection : IProfileNameProvider
    {
        public string Name { get; init; } = string.Empty;
    }
}