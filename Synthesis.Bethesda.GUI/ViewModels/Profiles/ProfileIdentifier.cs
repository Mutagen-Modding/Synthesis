using Mutagen.Bethesda;

namespace Synthesis.Bethesda.GUI
{
    public class ProfileIdentifier
    {
        public string ID { get; set; } = string.Empty;
        public string Nickname { get; set; } = string.Empty;
        public GameRelease Release { get; set; }
    }
}