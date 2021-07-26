using LibGit2Sharp;
using Noggog;

namespace Synthesis.Bethesda.Execution.GitRepository
{
    public interface ICloneRepo
    {
        DirectoryPath Clone(string repoPath, DirectoryPath localDir);
    }

    public class CloneRepo : ICloneRepo
    {
        public DirectoryPath Clone(string repoPath, DirectoryPath localDir)
        {
            return Repository.Clone(repoPath, localDir);
        }
    }
}