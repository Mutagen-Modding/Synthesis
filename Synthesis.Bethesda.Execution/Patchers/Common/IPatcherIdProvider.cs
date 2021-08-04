using System.Diagnostics.CodeAnalysis;

namespace Synthesis.Bethesda.Execution.Patchers.Common
{
    public interface IPatcherIdProvider
    {
        int InternalId { get; }
    }

    [ExcludeFromCodeCoverage]
    public record PatcherIdInjection(int InternalId) : IPatcherIdProvider;
}