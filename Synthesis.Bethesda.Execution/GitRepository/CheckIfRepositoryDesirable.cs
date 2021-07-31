using Serilog;

namespace Synthesis.Bethesda.Execution.GitRepository
{
    public interface ICheckIfRepositoryDesirable
    {
        bool IsDesirable(IGitRepository repo);
    }

    public class CheckIfRepositoryDesirable : ICheckIfRepositoryDesirable
    {
        private readonly ILogger _logger;

        public CheckIfRepositoryDesirable(ILogger logger)
        {
            _logger = logger;
        }
        
        public bool IsDesirable(IGitRepository repo)
        {
            var master = repo.MainBranch;
            if (master == null)
            {
                _logger.Warning("Could not locate master branch in {LocalDirectory}", repo.WorkingDirectory);
                return false;
            }
            return true;
        }
    }
}