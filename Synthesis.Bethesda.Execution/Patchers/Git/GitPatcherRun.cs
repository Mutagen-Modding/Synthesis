using LibGit2Sharp;
using Mutagen.Bethesda;
using Noggog;
using Synthesis.Bethesda.Execution.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Synthesis.Bethesda.Execution.Patchers.Git
{
    public class GitPatcherRun : IPatcherRun
    {
        public const string RunnerBranch = "SynthesisRunner";
        public string Name { get; }
        private readonly string _localDir;
        private GithubPatcherSettings _settings;
        public SolutionPatcherRun? SolutionRun { get; private set; }

        private Subject<string> _output = new Subject<string>();
        public IObservable<string> Output => _output;

        private Subject<string> _error = new Subject<string>();
        public IObservable<string> Error => _error;

        internal static readonly HashSet<string> MutagenLibraries;

        static GitPatcherRun()
        {
            MutagenLibraries = EnumExt.GetValues<GameCategory>()
                .Select(x => $"Mutagen.Bethesda.{x}")
                .And("Mutagen.Bethesda")
                .And("Mutagen.Bethesda.Core")
                .And("Mutagen.Bethesda.Kernel")
                .ToHashSet();
        }

        public GitPatcherRun(
            GithubPatcherSettings settings,
            string localDir)
        {
            _localDir = localDir;
            _settings = settings;
            Name = $"{settings.Nickname.Decorate(x => $"{x} => ")}{settings.RemoteRepoPath} => {Path.GetFileNameWithoutExtension(settings.SelectedProjectSubpath)}";
        }

        public void Dispose()
        {
        }

        public async Task Prep(GameRelease release, CancellationToken? cancel = null)
        {
            _output.OnNext("Cloning repository");
            var cloneResult = await CheckOrCloneRepo(GetResponse<string>.Succeed(_settings.RemoteRepoPath), _localDir, (x) => _output.OnNext(x), cancel ?? CancellationToken.None);
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

        public async Task Run(RunSynthesisPatcher settings, CancellationToken? cancel = null)
        {
            if (SolutionRun == null)
            {
                throw new SynthesisBuildFailure("Expected Solution Run object did not exist.");
            }
            using var outputSub = SolutionRun.Output.Subscribe(this._output);
            using var errSub = SolutionRun.Error.Subscribe(this._error);
            await SolutionRun.Run(settings, cancel).ConfigureAwait(false);
        }

        private static bool DeleteOldRepo(
            string localDir,
            GetResponse<string> remoteUrl,
            Action<string> logger)
        {
            if (!Directory.Exists(localDir))
            {
                logger("No local repository exists.  No cleaning to do.");
                return false;
            }
            var dirInfo = new DirectoryPath(localDir);
            if (remoteUrl.Failed)
            {
                logger("No remote repository.  Deleting local.");
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
                logger("Repository corrupted.  Deleting local.");
                dirInfo.DeleteEntireFolder();
                return false;
            }

            logger("Remote address targeted a different repository.  Deleting local.");
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

        public static string GetPathToSolution(string pathToRepo)
        {
            return Directory.EnumerateFiles(pathToRepo, "*.sln").FirstOrDefault();
        }

        public static string RunnerRepoDirectory(string profileID, string githubID)
        {
            return Path.Combine(Execution.Constants.WorkingDirectory, profileID, "Git", githubID, "Runner");
        }

        public static void SwapInDesiredVersionsForSolution(
            string solutionPath,
            string drivingProjSubPath,
            string? mutagenVersion,
            out string? listedMutagenVersion,
            string? synthesisVersion,
            out string? listedSynthesisVersion)
        {
            listedMutagenVersion = null;
            listedSynthesisVersion = null;
            foreach (var subProj in SolutionPatcherRun.AvailableProjects(solutionPath))
            {
                var proj = Path.Combine(Path.GetDirectoryName(solutionPath), subProj);
                var projXml = XElement.Parse(File.ReadAllText(proj));
                SwapInDesiredVersionsForProjectString(
                    projXml,
                    mutagenVersion: mutagenVersion,
                    listedMutagenVersion: out var curListedMutagenVersion,
                    synthesisVersion: synthesisVersion,
                    listedSynthesisVersion: out var curListedSynthesisVersion);
                TurnOffNullability(projXml);
                File.WriteAllText(proj, projXml.ToString());
                if (drivingProjSubPath.Equals(subProj))
                {
                    listedMutagenVersion = curListedMutagenVersion;
                    listedSynthesisVersion = curListedSynthesisVersion;
                }
            }
            foreach (var item in Directory.EnumerateFiles(Path.GetDirectoryName(solutionPath), "Directory.Build.props"))
            {
                var projXml = XElement.Parse(File.ReadAllText(item));
                TurnOffNullability(projXml);
                File.WriteAllText(item, projXml.ToString());
            }
        }

        public static void SwapInDesiredVersionsForProjectString(
            XElement proj,
            string? mutagenVersion,
            out string? listedMutagenVersion,
            string? synthesisVersion,
            out string? listedSynthesisVersion,
            bool addMissing = true)
        {
            listedMutagenVersion = null;
            listedSynthesisVersion = null;
            var missingLibs = new HashSet<string>(MutagenLibraries);
            XElement? itemGroup = null;
            foreach (var group in proj.Elements("ItemGroup"))
            {
                foreach (var elem in group.Elements().ToArray())
                {
                    if (!elem.Name.LocalName.Equals("PackageReference")) continue;
                    if (!elem.TryGetAttribute("Include", out var libAttr)) continue;
                    string swapInStr;
                    if (libAttr.Value.Equals("Mutagen.Bethesda.Synthesis"))
                    {
                        listedSynthesisVersion = elem.Attribute("Version")?.Value;
                        if (synthesisVersion == null) continue;
                        swapInStr = synthesisVersion;
                        missingLibs.Remove(libAttr.Value);
                    }
                    else if (MutagenLibraries.Contains(libAttr.Value))
                    {
                        listedMutagenVersion = elem.Attribute("Version")?.Value;
                        if (mutagenVersion == null) continue;
                        swapInStr = mutagenVersion;
                        missingLibs.Remove(libAttr.Value);
                    }
                    else
                    {
                        continue;
                    }
                    elem.SetAttributeValue("Version", swapInStr);
                }
                itemGroup = group;
            }
            if (itemGroup == null)
            {
                throw new ArgumentException("No ItemGroup found in project");
            }
            if (addMissing)
            {
                foreach (var missing in missingLibs)
                {
                    itemGroup.Add(new XElement("PackageReference",
                        new XAttribute("Include", missing),
                        new XAttribute("Version", mutagenVersion)));
                }
            }
        }

        public static void TurnOffNullability(XElement proj)
        {
            XElement? propGroup = null;
            foreach (var group in proj.Elements("PropertyGroup"))
            {
                foreach (var elem in group.Elements())
                {
                    if (elem.Name.LocalName.Equals("WarningsAsErrors"))
                    {
                        var warnings = elem.Value.Split(',');
                        elem.Value = string.Join(',', warnings.Where(x => !x.Contains("nullable", StringComparison.OrdinalIgnoreCase)));
                    }
                }
                propGroup = group;
            }
        }

        public static async Task<ConfigurationState<RunnerRepoInfo>> CheckoutRunnerRepository(
            string proj,
            string localRepoDir,
            GitPatcherVersioning patcherVersioning,
            NugetVersioningTarget nugetVersioning,
            Action<string>? logger,
            CancellationToken cancel)
        {
            try
            {
                cancel.ThrowIfCancellationRequested();

                logger?.Invoke($"Targeting {patcherVersioning}");

                using var repo = new Repository(localRepoDir);
                var runnerBranch = repo.Branches[RunnerBranch] ?? repo.CreateBranch(RunnerBranch);
                repo.Reset(ResetMode.Hard);
                Commands.Checkout(repo, runnerBranch);
                string? targetSha;
                string? target;
                switch (patcherVersioning.Versioning)
                {
                    case PatcherVersioningEnum.Tag:
                        if (string.IsNullOrWhiteSpace(patcherVersioning.Target)) return GetResponse<RunnerRepoInfo>.Fail("No tag selected");
                        targetSha = repo.Tags[patcherVersioning.Target]?.Target.Sha;
                        if (string.IsNullOrWhiteSpace(targetSha)) return GetResponse<RunnerRepoInfo>.Fail("Could not locate tag");
                        target = patcherVersioning.Target;
                        break;
                    case PatcherVersioningEnum.Commit:
                        targetSha = patcherVersioning.Target;
                        if (string.IsNullOrWhiteSpace(targetSha)) return GetResponse<RunnerRepoInfo>.Fail("Could not locate commit");
                        target = patcherVersioning.Target;
                        break;
                    case PatcherVersioningEnum.Branch:
                        if (string.IsNullOrWhiteSpace(patcherVersioning.Target)) return GetResponse<RunnerRepoInfo>.Fail($"Target branch had no name.");
                        var targetBranch = repo.Branches[$"origin/{patcherVersioning.Target}"];
                        if (targetBranch == null) return GetResponse<RunnerRepoInfo>.Fail($"Could not locate branch: {patcherVersioning.Target}");
                        targetSha = targetBranch.Tip.Sha;
                        target = patcherVersioning.Target;
                        break;
                    default:
                        throw new NotImplementedException();
                }
                if (!ObjectId.TryParse(targetSha, out var objId)) return GetResponse<RunnerRepoInfo>.Fail("Malformed sha string");

                cancel.ThrowIfCancellationRequested();
                var commit = repo.Lookup(objId, ObjectType.Commit) as Commit;
                if (commit == null) return GetResponse<RunnerRepoInfo>.Fail("Could not locate commit with given sha");

                cancel.ThrowIfCancellationRequested();
                var slnPath = GitPatcherRun.GetPathToSolution(localRepoDir);
                if (slnPath == null) return GetResponse<RunnerRepoInfo>.Fail("Could not locate solution to run.");

                var foundProjSubPath = SolutionPatcherRun.AvailableProject(slnPath, proj);

                if (foundProjSubPath == null) return GetResponse<RunnerRepoInfo>.Fail($"Could not locate target project file: {proj}.");

                cancel.ThrowIfCancellationRequested();
                logger?.Invoke($"Checking out {targetSha}");
                repo.Reset(ResetMode.Hard, commit, new CheckoutOptions());

                var projPath = Path.Combine(localRepoDir, foundProjSubPath);

                // Compile to help prep
                cancel.ThrowIfCancellationRequested();
                logger?.Invoke($"Mutagen Nuget: {nugetVersioning.MutagenVersioning} {nugetVersioning.MutagenVersion}");
                logger?.Invoke($"Synthesis Nuget: {nugetVersioning.SynthesisVersioning} {nugetVersioning.SynthesisVersion}");
                GitPatcherRun.SwapInDesiredVersionsForSolution(
                    slnPath,
                    drivingProjSubPath: foundProjSubPath,
                    mutagenVersion: nugetVersioning.MutagenVersioning == NugetVersioningEnum.Match ? null : nugetVersioning.MutagenVersion,
                    listedMutagenVersion: out var listedMutagenVersion,
                    synthesisVersion: nugetVersioning.SynthesisVersioning == NugetVersioningEnum.Match ? null : nugetVersioning.SynthesisVersion,
                    listedSynthesisVersion: out var listedSynthesisVersion);
                var compileResp = await SolutionPatcherRun.CompileWithDotnet(projPath, cancel);
                if (compileResp.Failed) return compileResp.BubbleFailure<RunnerRepoInfo>();

                return GetResponse<RunnerRepoInfo>.Succeed(
                    new RunnerRepoInfo(
                        slnPath: slnPath,
                        projPath: projPath,
                        target: target,
                        commitMsg: commit.Message,
                        commitDate: commit.Author.When.LocalDateTime,
                        listedSynthesis: listedSynthesisVersion,
                        listedMutagen: listedMutagenVersion));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return GetResponse<RunnerRepoInfo>.Fail(ex);
            }
        }
    }
}
