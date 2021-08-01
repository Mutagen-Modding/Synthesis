using System.Diagnostics.CodeAnalysis;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Environments.DI;

namespace Synthesis.Bethesda.Execution.Profile
{
    public interface IProfileIdentifier : IGameReleaseContext, IProfileNameProvider
    {
        string ID { get; }
    }

    [ExcludeFromCodeCoverage]
    public record ProfileIdentifier : IProfileIdentifier
    {
        public string ID { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public GameRelease Release { get; set; }
    }
}