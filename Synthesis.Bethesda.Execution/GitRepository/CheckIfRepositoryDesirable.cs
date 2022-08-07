using Noggog;
using Noggog.GitRepository;

namespace Synthesis.Bethesda.Execution.GitRepository;

public interface ICheckIfRepositoryDesirable
{
    ErrorResponse IsDesirable(IGitRepository repo);
}

public class CheckIfRepositoryDesirable : ICheckIfRepositoryDesirable
{
    public ErrorResponse IsDesirable(IGitRepository repo)
    {
        var master = repo.MainBranch;
        if (master == null)
        {
            return ErrorResponse.Fail($"Could not locate master branch in {repo.WorkingDirectory}");
        }
        return ErrorResponse.Success;
    }
}