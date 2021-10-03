using System.Diagnostics.CodeAnalysis;
using Noggog;

namespace Synthesis.Bethesda.Execution.Patchers.Git
{
    public interface IRunnerRepoDirectoryProvider
    {
        DirectoryPath Path { get; }
    }

    [ExcludeFromCodeCoverage]
    public class RunnerRepoDirectoryProvider : IRunnerRepoDirectoryProvider
    {
        private readonly IBaseRepoDirectoryProvider _baseRepoDir;

        public DirectoryPath Path => System.IO.Path.Combine(_baseRepoDir.Path, "Runner");
        
        public RunnerRepoDirectoryProvider(IBaseRepoDirectoryProvider baseRepoDir)
        {
            _baseRepoDir = baseRepoDir;
        }
    }

    [ExcludeFromCodeCoverage]
    public record RunnerRepoDirectoryInjection(DirectoryPath Path) : IRunnerRepoDirectoryProvider;
}