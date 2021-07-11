using Mutagen.Bethesda;
using Mutagen.Bethesda.Environments.DI;

namespace Synthesis.Bethesda.GUI.ViewModels.Profiles
{
    public interface IProfileIdentifier : IGameReleaseContext
    {
        string ID { get; }
        string Nickname { get; }
    }

    public class ProfileIdentifier : IProfileIdentifier
    {
        public string ID { get; set; } = string.Empty;
        public string Nickname { get; set; } = string.Empty;
        public GameRelease Release { get; set; }
    }
}