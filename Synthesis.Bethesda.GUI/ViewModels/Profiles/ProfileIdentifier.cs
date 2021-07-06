using Mutagen.Bethesda;
using Mutagen.Bethesda.Environments.DI;

namespace Synthesis.Bethesda.GUI
{
    public interface IProfileIdentifier : IGameReleaseContext
    {
        string ID { get; set; }
        string Nickname { get; set; }
    }

    public class ProfileIdentifier : IProfileIdentifier
    {
        public string ID { get; set; } = string.Empty;
        public string Nickname { get; set; } = string.Empty;
        public GameRelease Release { get; set; }
    }
}