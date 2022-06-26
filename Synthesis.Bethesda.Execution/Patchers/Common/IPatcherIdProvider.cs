using System.Diagnostics.CodeAnalysis;

namespace Synthesis.Bethesda.Execution.Patchers.Common;

public interface IPatcherIdProvider
{
    Guid InternalId { get; }
}

[ExcludeFromCodeCoverage]
public record PatcherIdInjection(Guid InternalId) : IPatcherIdProvider;