using System.Diagnostics.CodeAnalysis;
using LibGit2Sharp;
using Noggog;

namespace Synthesis.Bethesda.Execution.GitRepository
{
    public interface ICheckLocalRepoIsValid
    {
        bool IsValidRepository([NotNullWhen(true)]DirectoryPath? dir);
    }

    public class CheckLocalRepoIsValid : ICheckLocalRepoIsValid
    {
        public bool IsValidRepository([NotNullWhen(true)]DirectoryPath? dir)
        {
            if (dir == null) return false;
            return Repository.IsValid(dir);
        }
    }
}