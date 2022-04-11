using System.Diagnostics.CodeAnalysis;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Environments.DI;

namespace Synthesis.Bethesda.Execution.Profile;

public interface IProfileIdentifier : IGameReleaseContext
{
    string ID { get; }
}