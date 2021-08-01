using Noggog;
using Synthesis.Bethesda.Execution.Pathing;

namespace Synthesis.Bethesda.Execution.Patchers.TopLevel
{
    public interface IPatcherExtraDataPathProvider
    {
        DirectoryPath Path { get; }
    }

    public class PatcherExtraDataPathProvider : IPatcherExtraDataPathProvider
    {
        public IPatcherNameProvider NameProvider { get; }
        public IExtraDataPathProvider ExtraDataPathProvider { get; }

        public PatcherExtraDataPathProvider(
            IPatcherNameProvider nameProvider,
            IExtraDataPathProvider extraDataPathProvider)
        {
            NameProvider = nameProvider;
            ExtraDataPathProvider = extraDataPathProvider;
        }

        public DirectoryPath Path => System.IO.Path.Combine(ExtraDataPathProvider.Path, NameProvider.Name);
    }
}