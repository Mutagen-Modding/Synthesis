namespace Synthesis.Bethesda.Execution.Patchers.TopLevel
{
    public interface IPatcherIdProvider
    {
        int InternalId { get; }
    }

    public record PatcherIdInjection(int InternalId) : IPatcherIdProvider;
}