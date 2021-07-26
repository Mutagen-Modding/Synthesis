using System;
using System.Threading;
using LibGit2Sharp;
using Noggog;
using Serilog;

namespace Synthesis.Bethesda.Execution.GitRepository
{
    public interface ICheckOrCloneRepo
    {
        GetResponse<(string Remote, string Local)> Check(
            GetResponse<string> remote,
            DirectoryPath localDir,
            CancellationToken cancel);
    }

    public class CheckOrCloneRepo : ICheckOrCloneRepo
    {
        private readonly ILogger _logger;
        public IDeleteOldRepo Delete { get; }

        public CheckOrCloneRepo(
            ILogger logger,
            IDeleteOldRepo delete)
        {
            _logger = logger;
            Delete = delete;
        }
        
        public GetResponse<(string Remote, string Local)> Check(
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
                    return GetResponse<(string Remote, string Local)>.Succeed((remote.Value, localDir), remote.Reason);
                }
                cancel.ThrowIfCancellationRequested();
                if (remote.Failed) return GetResponse<(string Remote, string Local)>.Fail((remote.Value, string.Empty), remote.Reason);
                _logger.Information("Cloning remote {RemotePath}", remote.Value);
                var clonePath = Repository.Clone(remote.Value, localDir);
                return GetResponse<(string Remote, string Local)>.Succeed((remote.Value, clonePath), remote.Reason);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failure while checking/cloning repository");
                return GetResponse<(string Remote, string Local)>.Fail((remote.Value, string.Empty), ex);
            }
        }
    }
}