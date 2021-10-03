using LibGit2Sharp;
using Noggog;

namespace Synthesis.Bethesda.Execution.GitRepository
{
    public interface IGitRepositoryFactory
    {
        IGitRepository Get(DirectoryPath path);
    }

    public class GitRepositoryFactory : IGitRepositoryFactory
    {
        public IGitRepository Get(DirectoryPath path)
        {
            return new GitRepository(new Repository(path));
        }
    }
}