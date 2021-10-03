using System;
using System.Diagnostics.CodeAnalysis;
using LibGit2Sharp;
using Noggog;

namespace Synthesis.Bethesda.Execution.GitRepository
{
    public interface ICheckOriginRepoIsValid
    {
        bool IsValidRepository(string path);
    }

    [ExcludeFromCodeCoverage]
    public class CheckOriginRepoIsValid : ICheckOriginRepoIsValid
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