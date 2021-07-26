using LibGit2Sharp;
using Noggog;
using Serilog;

namespace Synthesis.Bethesda.Execution.GitRepository
{
    public interface IDeleteOldRepo
    {
        bool CheckIfKeeping(
            DirectoryPath localDir,
            GetResponse<string> remoteUrl);

        bool IsRepositoryUndesirable(IGitRepository repo);
    }

    public class DeleteOldRepo : IDeleteOldRepo
    {
        private readonly ILogger _Logger;
        public IProvideRepositoryCheckouts RepoCheckouts { get; }

        public DeleteOldRepo(
            ILogger logger,
            IProvideRepositoryCheckouts repoCheckouts)
        {
            _Logger = logger;
            RepoCheckouts = repoCheckouts;
        }
        
        public bool CheckIfKeeping(
            DirectoryPath localDir,
            GetResponse<string> remoteUrl)
        {
            if (!localDir.Exists)
            {
                _Logger.Information("No local repository exists at {LocalDirectory}.  No cleaning to do.", localDir);
                return false;
            }
            if (remoteUrl.Failed)
            {
                _Logger.Warning("No remote repository.  Deleting local at {LocalDirectory}.", localDir);
                localDir.DeleteEntireFolder();
                return false;
            }
            try
            {
                using var repoCheckout = RepoCheckouts.Get(localDir);
                var repo = repoCheckout.Repository;

                if (IsRepositoryUndesirable(repo)) return true;
                
                // If it's the same remote repo, don't delete
                if (repo.MainRemoteUrl?.Equals(remoteUrl.Value) ?? false)
                {
                    _Logger.Information("Remote repository target matched local folder's repo at {LocalDirectory}.  Keeping clone.", localDir);
                    return true;
                }
            }
            catch (RepositoryNotFoundException)
            {
                _Logger.Error("Repository corrupted.  Deleting local at {LocalDirectory}.", localDir);
                localDir.DeleteEntireFolder();
                return false;
            }

            _Logger.Information("Remote address targeted a different repository.  Deleting local at {LocalDirectory}", localDir);
            localDir.DeleteEntireFolder();
            return false;
        }

        public bool IsRepositoryUndesirable(IGitRepository repo)
        {
            var master = repo.MainBranch;
            if (master == null)
            {
                _Logger.Warning("Could not locate master branch in {LocalDirectory}", repo.WorkingDirectory);
                return true;
            }
            return false;
        }
    }
}