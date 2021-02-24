using LibGit2Sharp;
using Noggog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Synthesis.Bethesda.Execution
{
    public static class GitUtility
    {
        private static bool DeleteOldRepo(
            string localDir,
            GetResponse<string> remoteUrl,
            Action<string> logger)
        {
            if (!Directory.Exists(localDir))
            {
                logger($"No local repository exists at {localDir}.  No cleaning to do.");
                return false;
            }
            var dirInfo = new DirectoryPath(localDir);
            if (remoteUrl.Failed)
            {
                logger($"No remote repository.  Deleting local at {localDir}.");
                dirInfo.DeleteEntireFolder();
                return false;
            }
            try
            {
                using var repo = new Repository(localDir);
                // If it's the same remote repo, don't delete
                if (repo.Network.Remotes.FirstOrDefault()?.Url.Equals(remoteUrl.Value) ?? false)
                {
                    logger("Remote repository target matched local folder's repo.  Keeping clone.");
                    return true;
                }
            }
            catch (RepositoryNotFoundException)
            {
                logger($"Repository corrupted.  Deleting local at {localDir}");
                dirInfo.DeleteEntireFolder();
                return false;
            }

            logger($"Remote address targeted a different repository.  Deleting local at {localDir}");
            dirInfo.DeleteEntireFolder();
            return false;
        }

        public static async Task<GetResponse<(string Remote, string Local)>> CheckOrCloneRepo(
            GetResponse<string> remote,
            string localDir,
            Action<string> logger,
            CancellationToken cancel)
        {
            try
            {
                cancel.ThrowIfCancellationRequested();
                if (DeleteOldRepo(localDir: localDir, remoteUrl: remote, logger: logger))
                {
                    // Short circuiting deletion
                    return GetResponse<(string Remote, string Local)>.Succeed((remote.Value, localDir), remote.Reason);
                }
                cancel.ThrowIfCancellationRequested();
                if (remote.Failed) return GetResponse<(string Remote, string Local)>.Fail((remote.Value, string.Empty), remote.Reason);
                logger($"Cloning remote {remote.Value}");
                var clonePath = Repository.Clone(remote.Value, localDir);
                return GetResponse<(string Remote, string Local)>.Succeed((remote.Value, clonePath), remote.Reason);
            }
            catch (Exception ex)
            {
                logger($"Failure while checking/cloning repository: {ex}");
                return GetResponse<(string Remote, string Local)>.Fail((remote.Value, string.Empty), ex);
            }
        }
    }
}
