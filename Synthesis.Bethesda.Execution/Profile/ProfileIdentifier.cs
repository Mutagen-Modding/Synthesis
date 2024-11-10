namespace Synthesis.Bethesda.Execution.Profile;

public interface IProfileIdentifier
{
    string ID { get; }
}

public record ProfileIdentifier(string ID) : IProfileIdentifier;