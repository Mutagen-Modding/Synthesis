using System;
using LibGit2Sharp;
using Noggog;

namespace Synthesis.Bethesda.Execution.Patchers.Git
{
    public interface ICheckRepoIsValid
    {
        bool IsValidRepository(string path);
    }

    public class CheckRepoIsValid : ICheckRepoIsValid
    {
        public bool IsValidRepository(string path)
        {
            try
            {
                if (Repository.ListRemoteReferences(path).Any()) return true;
            }
            catch (Exception)
            {
            }

            return false;
        }
    }
}