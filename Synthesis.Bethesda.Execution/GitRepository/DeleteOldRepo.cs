using System;
using System.Linq;
using LibGit2Sharp;
using Noggog;

namespace Synthesis.Bethesda.Execution.GitRespository
{
    public interface IDeleteOldRepo
    {
        bool Delete(
            DirectoryPath localDir,
            GetResponse<string> remoteUrl,
            Action<string> logger);

        bool IsRepositoryUndesirable(
            Repository repo,
            Action<string> logger);
    }

    public class DeleteOldRepo : IDeleteOldRepo
    {
        public bool Delete(
            DirectoryPath localDir,
            GetResponse<string> remoteUrl,
            Action<string> logger)
        {
            if (!localDir.Exists)
            {
                logger($"No local repository exists at {localDir}.  No cleaning to do.");
                return false;
            }
            if (remoteUrl.Failed)
            {
                logger($"No remote repository.  Deleting local at {localDir}.");
                localDir.DeleteEntireFolder();
                return false;
            }
            try
            {
                using var repo = new Repository(localDir);

                if (IsRepositoryUndesirable(repo, logger)) return true;
                
                // If it's the same remote repo, don't delete
                if (repo.Network.Remotes.FirstOrDefault()?.Url.Equals(remoteUrl.Value) ?? false)
                {
                    logger($"Remote repository target matched local folder's repo at {localDir}.  Keeping clone.");
                    return true;
                }
            }
            catch (RepositoryNotFoundException)
            {
                logger($"Repository corrupted.  Deleting local at {localDir}");
                localDir.DeleteEntireFolder();
                return false;
            }

            logger($"Remote address targeted a different repository.  Deleting local at {localDir}");
            localDir.DeleteEntireFolder();
            return false;
        }

        public bool IsRepositoryUndesirable(
            Repository repo,
            Action<string> logger)
        {
            var master = repo.Branches.Where(b => b.IsCurrentRepositoryHead).FirstOrDefault();
            if (master == null)
            {
                logger($"{repo.Info.Path}: Could not locate master branch");
                return true;
            }
            return false;
        }
    }
}