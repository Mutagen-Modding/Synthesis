using System;
using System.Threading;
using Noggog;
using Serilog;

namespace Synthesis.Bethesda.Execution.GitRepository
{
    public record RepoPathPair(string Remote, DirectoryPath Local);
    
    public interface ICheckOrCloneRepo
    {
        GetResponse<RepoPathPair> Check(
            GetResponse<string> remote,
            DirectoryPath localDir,
            CancellationToken cancel);
    }

    public class CheckOrCloneRepo : ICheckOrCloneRepo
    {
        private readonly ILogger _logger;
        public ICloneRepo CloneRepo { get; }
        public IDeleteOldRepo Delete { get; }

        public CheckOrCloneRepo(
            ILogger logger,
            ICloneRepo cloneRepo,
            IDeleteOldRepo delete)
        {
            _logger = logger;
            CloneRepo = cloneRepo;
            Delete = delete;
        }
        
        public GetResponse<RepoPathPair> Check(
            GetResponse<string> remote,
            DirectoryPath localDir,
            CancellationToken cancel)
        {
            try
            {
                cancel.ThrowIfCancellationRequested();
                if (Delete.CheckIfKeeping(localDir: localDir, remoteUrl: remote))
                {
                    // Short circuiting deletion
                    return GetResponse<RepoPathPair>.Succeed(new(remote.Value, localDir), remote.Reason);
                }
                cancel.ThrowIfCancellationRequested();
                if (remote.Failed) return GetResponse<RepoPathPair>.Fail(new(remote.Value, string.Empty), remote.Reason);
                _logger.Information("Cloning remote {RemotePath}", remote.Value);
                var clonePath = CloneRepo.Clone(remote.Value, localDir);
                return GetResponse<RepoPathPair>.Succeed(new(remote.Value, clonePath), remote.Reason);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failure while checking/cloning repository");
                return GetResponse<RepoPathPair>.Fail(new(remote.Value, string.Empty), ex);
            }
        }
    }
}