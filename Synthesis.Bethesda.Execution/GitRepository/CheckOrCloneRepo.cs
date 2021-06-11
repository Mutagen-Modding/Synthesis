using System;
using System.Threading;
using System.Threading.Tasks;
using LibGit2Sharp;
using Noggog;

namespace Synthesis.Bethesda.Execution.GitRespository
{
    public interface ICheckOrCloneRepo
    {
        GetResponse<(string Remote, string Local)> Check(
            GetResponse<string> remote,
            DirectoryPath localDir,
            Action<string> logger,
            CancellationToken cancel);
    }

    public class CheckOrCloneRepo : ICheckOrCloneRepo
    {
        private readonly IDeleteOldRepo _Delete;

        public CheckOrCloneRepo(IDeleteOldRepo delete)
        {
            _Delete = delete;
        }
        
        public GetResponse<(string Remote, string Local)> Check(
            GetResponse<string> remote,
            DirectoryPath localDir,
            Action<string> logger,
            CancellationToken cancel)
        {
            try
            {
                cancel.ThrowIfCancellationRequested();
                if (_Delete.CheckIfKeeping(localDir: localDir, remoteUrl: remote))
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