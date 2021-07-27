using LibGit2Sharp;

namespace Synthesis.Bethesda.Execution.GitRepository
{
    public interface IBranch
    {
        string RemoteName { get; }
        string FriendlyName { get; }
        ICommit Tip { get; }
        Branch GetUnderlying();
    }

    public class BranchWrapper : IBranch
    {
        private readonly Branch _branch;
        public string RemoteName => _branch.RemoteName;
        public string FriendlyName => _branch.FriendlyName;
        public ICommit Tip => new CommitWrapper(_branch.Tip);

        public BranchWrapper(Branch branch)
        {
            _branch = branch;
        }

        public Branch GetUnderlying() => _branch;
    }
}