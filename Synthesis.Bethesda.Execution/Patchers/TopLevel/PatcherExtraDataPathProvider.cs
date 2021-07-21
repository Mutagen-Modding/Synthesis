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
        private readonly IPatcherNameProvider _nameProvider;
        private readonly IExtraDataPathProvider _extraDataPathProvider;

        public PatcherExtraDataPathProvider(
            IPatcherNameProvider nameProvider,
            IExtraDataPathProvider extraDataPathProvider)
        {
            _nameProvider = nameProvider;
            _extraDataPathProvider = extraDataPathProvider;
        }

        public DirectoryPath Path => System.IO.Path.Combine(_extraDataPathProvider.Path, _nameProvider.Name);
    }
}