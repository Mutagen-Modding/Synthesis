using Serilog;
using Synthesis.Bethesda.Execution.GitRepository;
using Synthesis.Bethesda.Execution.Patchers.Git;

namespace Synthesis.Bethesda.Execution.Patchers.Running.Solution
{
    public interface IPrintShaIfApplicable
    {
        void Print();
    }

    public class PrintShaIfApplicable : IPrintShaIfApplicable
    {
        private readonly ILogger _logger;
        public ICheckLocalRepoIsValid LocalRepoIsValid { get; }
        public IProvideRepositoryCheckouts RepositoryCheckouts { get; }
        public IPathToRepositoryProvider PathToRepositoryProvider { get; }

        public PrintShaIfApplicable(
            ILogger logger,
            ICheckLocalRepoIsValid localRepoIsValid,
            IProvideRepositoryCheckouts repositoryCheckouts,
            IPathToRepositoryProvider pathToRepositoryProvider)
        {
            _logger = logger;
            LocalRepoIsValid = localRepoIsValid;
            RepositoryCheckouts = repositoryCheckouts;
            PathToRepositoryProvider = pathToRepositoryProvider;
        }
        
        public void Print()
        {
            var repoPath = PathToRepositoryProvider.Path;
            if (LocalRepoIsValid.IsValidRepository(repoPath))
            {
                using var repo = RepositoryCheckouts.Get(repoPath.Value);
                _logger.Information("Sha {Sha}", repo.Repository.CurrentSha);
            }
        }
    }
}