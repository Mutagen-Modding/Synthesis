using Noggog;
using Synthesis.Bethesda.Execution.Pathing;
using Synthesis.Bethesda.Execution.Profile;

namespace Synthesis.Bethesda.Execution.Patchers.Git
{
    public interface IRunnerRepoDirectoryProvider
    {
        DirectoryPath Path { get; }
    }

    public class RunnerRepoDirectoryProvider : IRunnerRepoDirectoryProvider
    {
        private readonly IProvideWorkingDirectory _workingDirectory;
        private readonly IProfileIdentifier _identifier;
        private readonly IGithubPatcherIdentifier _githubId;

        public DirectoryPath Path => System.IO.Path.Combine(
            _workingDirectory.WorkingDirectory,
            _identifier.ID,
            "Git",
            _githubId.Id, 
            "Runner");
        
        public RunnerRepoDirectoryProvider(
            IProvideWorkingDirectory workingDirectory,
            IProfileIdentifier identifier,
            IGithubPatcherIdentifier githubId)
        {
            _workingDirectory = workingDirectory;
            _identifier = identifier;
            _githubId = githubId;
        }
    }
}