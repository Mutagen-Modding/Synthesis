using System.Diagnostics.CodeAnalysis;
using Synthesis.Bethesda.Execution.GitRepository;

namespace Synthesis.Bethesda.Execution.Patchers.Git.PrepareDriver
{
    public interface IGetToLatestMaster
    {
        bool TryGet(IGitRepository repo, [MaybeNullWhen(false)] out string branchName);
    }

    public class GetToLatestMaster : IGetToLatestMaster
    {
        public bool TryGet(IGitRepository repo, [MaybeNullWhen(false)] out string branchName)
        {
            var master = repo.MainBranch;
            if (master == null)
            {
                branchName = default;
                return false;
            }

            branchName = master.FriendlyName;
            repo.ResetHard();
            repo.Checkout(master);
            repo.Pull();
            return true;
        }
    }
}