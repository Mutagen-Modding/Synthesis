using Mutagen.Bethesda;

namespace Synthesis.Bethesda.GUI
{
    public interface IProfileIdentifier
    {
        string ID { get; set; }
        string Nickname { get; set; }
        GameRelease Release { get; set; }
    }

    public class ProfileIdentifier : IProfileIdentifier
    {
        public string ID { get; set; } = string.Empty;
        public string Nickname { get; set; } = string.Empty;
        public GameRelease Release { get; set; }
    }
}