using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Noggog;
using Noggog.IO;
using Serilog;

namespace Synthesis.Bethesda.Execution.GitRepository
{
    [ExcludeFromCodeCoverage]
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
        public IDeleteEntireDirectory DeleteEntireDirectory { get; }
        public ICheckIfKeeping ShouldKeep { get; }

        public CheckOrCloneRepo(
            ILogger logger,
            ICloneRepo cloneRepo,
            IDeleteEntireDirectory deleteEntireDirectory,
            ICheckIfKeeping shouldKeep)
        {
            _logger = logger;
            CloneRepo = cloneRepo;
            DeleteEntireDirectory = deleteEntireDirectory;
            ShouldKeep = shouldKeep;
        }
        
        public GetResponse<RepoPathPair> Check(
            GetResponse<string> remote,
            DirectoryPath localDir,
            CancellationToken cancel)
        {
            try
            {
                cancel.ThrowIfCancellationRequested();
                if (ShouldKeep.ShouldKeep(localDir: localDir, remoteUrl: remote))
                {
                    return GetResponse<RepoPathPair>.Succeed(new(remote.Value, localDir), remote.Reason);
                }
                cancel.ThrowIfCancellationRequested();
                
                DeleteEntireDirectory.DeleteEntireFolder(localDir);
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