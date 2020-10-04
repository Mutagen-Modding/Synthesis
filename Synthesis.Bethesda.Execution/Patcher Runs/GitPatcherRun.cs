using LibGit2Sharp;
using Mutagen.Bethesda;
using Noggog;
using Synthesis.Bethesda.Execution.Patchers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Synthesis.Bethesda.Execution
{
    public class GitPatcherRun : IPatcherRun
    {
        public string Name { get; }
        private readonly string _nickname;
        private readonly string _remote;
        private readonly string _localDir;
        private readonly string _pathToSln;
        private readonly string _pathToProj;
        private readonly string _pathToExe;
        public SolutionPatcherRun? SolutionRun { get; private set; }

        private Subject<string> _output = new Subject<string>();
        public IObservable<string> Output => _output;

        private Subject<string> _error = new Subject<string>();
        public IObservable<string> Error => _error;

        public GitPatcherRun(string nickname, string remote, string localDir, string pathToSln, string pathToProj, string pathToExe)
        {
            _nickname = nickname;
            _remote = remote;
            _localDir = localDir;
            _pathToProj = pathToProj;
            _pathToSln = pathToSln;
            _pathToExe = pathToExe;
            Name = $"{nickname} => {remote} => {Path.GetFileNameWithoutExtension(pathToProj)}";
        }

        public void Dispose()
        {
        }

        public async Task Prep(GameRelease release, CancellationToken? cancel = null)
        {
            var prepResult = await PrepRepo(GetResponse<string>.Succeed(_remote), _localDir, cancel ?? CancellationToken.None);
            if (prepResult.Failed)
            {
                throw new SynthesisBuildFailure(prepResult.Reason);
            }
            SolutionRun = new SolutionPatcherRun(_nickname, Path.Combine(_localDir, _pathToSln), Path.Combine(_localDir, _pathToProj), Path.Combine(_localDir, _pathToExe));
            await SolutionRun.Prep(release, cancel).ConfigureAwait(false);
        }

        public async Task Run(RunSynthesisPatcher settings, CancellationToken? cancel = null)
        {
            if (SolutionRun == null)
            {
                throw new SynthesisBuildFailure("Expected Solution Run object did not exist.");
            }
            await SolutionRun.Run(settings, cancel).ConfigureAwait(false);
        }

        private static bool DeleteOldRepo(string localDir, GetResponse<string> remoteUrl)
        {
            if (!Directory.Exists(localDir)) return false;
            var dirInfo = new DirectoryPath(localDir);
            if (remoteUrl.Failed)
            {
                dirInfo.DeleteEntireFolder();
                return false;
            }
            using var repo = new Repository(localDir);
            // If it's the same remote repo, don't delete
            if (repo.Network.Remotes.FirstOrDefault()?.Url.Equals(remoteUrl.Value) ?? false) return true;
            dirInfo.DeleteEntireFolder();
            return false;
        }

        public static async Task<GetResponse<(string Remote, string Local)>> PrepRepo(GetResponse<string> remote, string localDir, CancellationToken cancel)
        {
            try
            {
                cancel.ThrowIfCancellationRequested();
                if (DeleteOldRepo(localDir: localDir, remoteUrl: remote))
                {
                    // Short circuiting deletion
                    return GetResponse<(string Remote, string Local)>.Succeed((remote.Value, localDir), remote.Reason);
                }
                cancel.ThrowIfCancellationRequested();
                if (remote.Failed) return GetResponse<(string Remote, string Local)>.Fail((remote.Value, string.Empty), remote.Reason);
                var clonePath = Repository.Clone(remote.Value, localDir);
                return GetResponse<(string Remote, string Local)>.Succeed((remote.Value, clonePath), remote.Reason);
            }
            catch (Exception ex)
            {
                return GetResponse<(string Remote, string Local)>.Fail((remote.Value, string.Empty), ex);
            }
        }
    }
}
