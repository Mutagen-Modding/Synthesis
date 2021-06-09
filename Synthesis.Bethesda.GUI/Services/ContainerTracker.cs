using StructureMap;

namespace Synthesis.Bethesda.GUI.Services
{
    public interface IContainerTracker
    {
        IContainer Container { get; set; }
    }

    public class ContainerTracker : IContainerTracker
    {
        public IContainer Container { get; set; } = null!;
    }
}