using System.Diagnostics.CodeAnalysis;
using Noggog;

namespace Synthesis.Bethesda.Execution.Patchers.Git
{
    public interface IBuildMetaFilePathProvider
    {
        FilePath Path { get; }
    }

    [ExcludeFromCodeCoverage]
    public class BuildMetaFilePathProvider : IBuildMetaFilePathProvider
    {
        private readonly IBaseRepoDirectoryProvider _baseRepoDir;

        public FilePath Path => System.IO.Path.Combine(_baseRepoDir.Path, "Build.meta");
        
        public BuildMetaFilePathProvider(IBaseRepoDirectoryProvider baseRepoDir)
        {
            _baseRepoDir = baseRepoDir;
        }
    }

    [ExcludeFromCodeCoverage]
    public record BuildMetaFilePathProviderInjection(FilePath Path) : IBuildMetaFilePathProvider;
}