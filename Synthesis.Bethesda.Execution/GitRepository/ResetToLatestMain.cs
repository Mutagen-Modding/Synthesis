using System.Diagnostics.CodeAnalysis;
using Noggog;

namespace Synthesis.Bethesda.Execution.GitRepository
{
    public interface IResetToLatestMain
    {
        GetResponse<IBranch> TryReset(IGitRepository repo);
    }

    public class ResetToLatestMain : IResetToLatestMain
    {
        public GetResponse<IBranch> TryReset(IGitRepository repo)
        {
            var master = repo.MainBranch;
            if (master == null)
            {
                return GetResponse<IBranch>.Fail("Could not find main branch");
            }

            repo.ResetHard();
            repo.Checkout(master);
            repo.Pull();
            return GetResponse<IBranch>.Succeed(master);
        }
    }
}