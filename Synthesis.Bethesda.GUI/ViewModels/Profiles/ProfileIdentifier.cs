using Mutagen.Bethesda;
using Mutagen.Bethesda.Environments.DI;
using StructureMap;

namespace Synthesis.Bethesda.GUI
{
    public interface IProfileIdentifier : IGameReleaseContext
    {
        string ID { get; }
        string Nickname { get; }
        public IContainer Container { get; }
    }

    public class ProfileIdentifier : IProfileIdentifier
    {
        public string ID { get; set; } = string.Empty;
        public string Nickname { get; set; } = string.Empty;
        public GameRelease Release { get; set; }
        public IContainer Container { get; }

        public ProfileIdentifier(IContainer cont)
        {
            Container = cont;
        }
    }
}