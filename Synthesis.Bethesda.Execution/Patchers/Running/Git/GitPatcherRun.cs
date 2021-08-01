using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using Noggog;
using Serilog;
using Synthesis.Bethesda.Execution.GitRepository;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Patchers.Running.Solution;
using Synthesis.Bethesda.Execution.Patchers.Solution;
using Synthesis.Bethesda.Execution.Settings;

namespace Synthesis.Bethesda.Execution.Patchers.Running.Git
{
    public interface IGitPatcherRun : IPatcherRun
    {
    }

    [ExcludeFromCodeCoverage]
    public class GitPatcherRun : IGitPatcherRun
    {
        public string Name { get; }
        private readonly GithubPatcherSettings _settings;
        private readonly ILogger _logger;
        private readonly IRunnerRepoDirectoryProvider _runnerRepoDirectoryProvider;
        private readonly IAvailableProjectsRetriever _availableProjectsRetriever;
        private readonly ICheckOrCloneRepo _checkOrClone;
        public SolutionPatcherRun? SolutionRun { get; private set; }
        private readonly CompositeDisposable _disposable = new();

        public GitPatcherRun(
            GithubPatcherSettings settings,
            ILogger logger,
            IRunnerRepoDirectoryProvider runnerRepoDirectoryProvider,
            IAvailableProjectsRetriever availableProjectsRetriever,
            ICheckOrCloneRepo checkOrClone)
        {
            _settings = settings;
            _logger = logger;
            _runnerRepoDirectoryProvider = runnerRepoDirectoryProvider;
            _availableProjectsRetriever = availableProjectsRetriever;
            _checkOrClone = checkOrClone;
            Name = $"{settings.Nickname.Decorate(x => $"{x} => ")}{settings.RemoteRepoPath} => {Path.GetFileNameWithoutExtension(settings.SelectedProjectSubpath)}";
        }

        public void AddForDisposal(IDisposable disposable)
        {
            _disposable.Add(disposable);
        }

        public void Dispose()
        {
            _disposable.Dispose();
        }

        public async Task Prep(CancellationToken cancel)
        {
            _logger.Information("Cloning repository");
            var cloneResult = _checkOrClone.Check(
                GetResponse<string>.Succeed(_settings.RemoteRepoPath),
                _runnerRepoDirectoryProvider.Path,
                cancel);
            if (cloneResult.Failed)
            {
                throw new SynthesisBuildFailure(cloneResult.Reason);
            }

            throw new NotImplementedException("Need to migrate in proper git checkouts");

            //_output.OnNext($"Locating path to solution based on local dir {_localDir}");
            //var pathToSln = GetPathToSolution(_localDir);
            //_output.OnNext($"Locating path to project based on {pathToSln} AND {_settings.SelectedProjectSubpath}");
            //var foundProjSubPath = SolutionPatcherRun.AvailableProject(pathToSln, _settings.SelectedProjectSubpath);
            //if (foundProjSubPath == null)
            //{
            //    throw new SynthesisBuildFailure("Could not locate project sub path");
            //}
            //var pathToProj = Path.Combine(_localDir, foundProjSubPath);
            //SolutionRun = new SolutionPatcherRun(
            //    _settings.Nickname,
            //    pathToSln: Path.Combine(_localDir, pathToSln), 
            //    pathToProj: pathToProj);
            //using var outputSub = SolutionRun.Output.Subscribe(this._output);
            //using var errSub = SolutionRun.Error.Subscribe(this._error);
            //await SolutionRun.Prep(release, cancel).ConfigureAwait(false);
        }

        public async Task Run(RunSynthesisPatcher settings, CancellationToken cancel)
        {
            if (SolutionRun == null)
            {
                throw new SynthesisBuildFailure("Expected Solution Run object did not exist.");
            }
            await SolutionRun.Run(settings, cancel).ConfigureAwait(false);
        }
    }
}
