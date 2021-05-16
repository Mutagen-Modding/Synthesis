using LibGit2Sharp;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Noggog;
using NuGet.Versioning;
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
        public readonly static System.Version NewtonSoftAddMutaVersion = new(0, 26);
        public readonly static System.Version NewtonSoftAddSynthVersion = new(0, 14, 1);
        public readonly static System.Version NewtonSoftRemoveMutaVersion = new(0, 28);
        public readonly static System.Version NewtonSoftRemoveSynthVersion = new(0, 17, 5);
        public readonly static System.Version NamespaceMutaVersion = new(0, 29, 2, 1);
        public string Name { get; }
        private readonly string _localDir;
        private readonly GithubPatcherSettings _settings;
        public SolutionPatcherRun? SolutionRun { get; private set; }

        private readonly Subject<string> _output = new();
        public IObservable<string> Output => _output;

        private readonly Subject<string> _error = new();
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

        public async Task Prep(GameRelease release, CancellationToken cancel)
        {
            _output.OnNext("Cloning repository");
            var cloneResult = await GitUtility.CheckOrCloneRepo(GetResponse<string>.Succeed(_settings.RemoteRepoPath), _localDir, (x) => _output.OnNext(x), cancel);
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
            using var outputSub = SolutionRun.Output.Subscribe(this._output);
            using var errSub = SolutionRun.Error.Subscribe(this._error);
            await SolutionRun.Run(settings, cancel).ConfigureAwait(false);
        }

        public static string? GetPathToSolution(string pathToRepo)
        {
            return Directory.EnumerateFiles(pathToRepo, "*.sln").FirstOrDefault();
        }

        public static string RunnerRepoDirectory(string profileID, string githubID)
        {
            return Path.Combine(Execution.Paths.WorkingDirectory, profileID, "Git", githubID, "Runner");
        }

        public static void ModifyProject(
            string solutionPath,
            string drivingProjSubPath,
            string? mutagenVersion,
            out string? listedMutagenVersion,
            string? synthesisVersion,
            out string? listedSynthesisVersion)
        {
            listedMutagenVersion = null;
            listedSynthesisVersion = null;

            string? TrimVersion(string? version)
            {
                if (version == null) return null;
                var index = version.IndexOf('-');
                if (index == -1) return version;
                return version.Substring(0, index);
            }

            var trimmedMutagenVersion = TrimVersion(mutagenVersion);
            var trimmedsynthesisVersion = TrimVersion(synthesisVersion);
            foreach (var subProj in SolutionPatcherRun.AvailableProjects(solutionPath))
            {
                var proj = Path.Combine(Path.GetDirectoryName(solutionPath)!, subProj);
                var projXml = XElement.Parse(File.ReadAllText(proj));
                SwapInDesiredVersionsForProjectString(
                    projXml,
                    mutagenVersion: mutagenVersion,
                    listedMutagenVersion: out var curListedMutagenVersion,
                    synthesisVersion: synthesisVersion,
                    listedSynthesisVersion: out var curListedSynthesisVersion);
                TurnOffNullability(projXml);
                RemoveGitInfo(projXml);
                SwapOffNetCore(projXml);
                TurnOffWindowsSpecificationInTargetFramework(projXml);
                System.Version.TryParse(curListedMutagenVersion, out var mutaVersion);
                System.Version.TryParse(curListedSynthesisVersion, out var synthVersion);
                if ((mutaVersion != null
                    && mutaVersion <= NewtonSoftAddMutaVersion)
                    || (synthVersion != null
                        && synthVersion <= NewtonSoftAddSynthVersion))
                {
                    AddNewtonsoftToOldSetups(projXml);
                }
                System.Version.TryParse(trimmedMutagenVersion, out var targetMutaVersion);
                System.Version.TryParse(trimmedsynthesisVersion, out var targetSynthesisVersion);
                if ((targetMutaVersion != null
                    && targetMutaVersion >= NewtonSoftRemoveMutaVersion)
                    || (targetSynthesisVersion != null
                        && targetSynthesisVersion >= NewtonSoftRemoveSynthVersion))
                {
                    RemovePackage(projXml, "Newtonsoft.Json");
                }

                if (targetMutaVersion >= NamespaceMutaVersion
                    && mutaVersion < NamespaceMutaVersion)
                {
                    ProcessProjUsings(proj);
                    SwapVersioning(projXml, "Mutagen.Bethesda.FormKeys.SkyrimSE", "2.0.0.1-dev", new SemanticVersion(2, 0, 0));
                }

                File.WriteAllText(proj, projXml.ToString());

                if (drivingProjSubPath.Equals(subProj))
                {
                    listedMutagenVersion = curListedMutagenVersion;
                    listedSynthesisVersion = curListedSynthesisVersion;
                }
            }
            foreach (var item in Directory.EnumerateFiles(Path.GetDirectoryName(solutionPath)!, "Directory.Build.*"))
            {
                var projXml = XElement.Parse(File.ReadAllText(item));
                TurnOffNullability(projXml);
                File.WriteAllText(item, projXml.ToString());
            }
        }
        public static void RemovePackage(
            XElement proj,
            string packageName)
        {
            foreach (var group in proj.Elements("ItemGroup"))
            {
                foreach (var elem in group.Elements().ToArray())
                {
                    if (!elem.Name.LocalName.Equals("PackageReference")) continue;
                    if (!elem.TryGetAttribute("Include", out var libAttr)) continue;
                    if (!libAttr.Value.Equals(packageName)) continue;
                    elem.Remove();
                }
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
            if (addMissing && mutagenVersion != null)
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

        public static void SwapOffNetCore(XElement proj)
        {
            foreach (var group in proj.Elements("PropertyGroup"))
            {
                foreach (var elem in group.Elements())
                {
                    if (elem.Name.LocalName.Equals("TargetFramework")
                        && elem.Value.Equals("netcoreapp3.1"))
                    {
                        elem.Value = "net5.0";
                    }
                }
            }
        }

        public static void TurnOffWindowsSpecificationInTargetFramework(XElement proj)
        {
            foreach (var group in proj.Elements("PropertyGroup"))
            {
                foreach (var elem in group.Elements())
                {
                    if (elem.Name.LocalName.Equals("TargetFramework")
                        && elem.Value.EndsWith("-windows7.0"))
                    {
                        elem.Value = elem.Value.TrimEnd("-windows7.0");
                    }
                }
            }
        }

        public static void AddNewtonsoftToOldSetups(XElement proj)
        {
            foreach (var group in proj.Elements("ItemGroup"))
            {
                foreach (var elem in group.Elements())
                {
                    if (!elem.Name.LocalName.Equals("PackageReference")) continue;
                    if (!elem.TryGetAttribute("Include", out var include)) continue;
                    if (include.Equals("Newtonsoft.Json")) return;
                }
            }

            proj.Add(new XElement("ItemGroup",
                new XElement("PackageReference",
                    new XAttribute("Include", "Newtonsoft.Json"),
                    new XAttribute("Version", Versions.NewtonsoftVersion))));
        }

        public static void RemoveGitInfo(XElement proj)
        {
            foreach (var group in proj.Elements("ItemGroup"))
            {
                foreach (var elem in group.Elements("PackageReference").ToList())
                {
                    if (elem.TryGetAttributeString("Include", out var includeAttr)
                        && includeAttr == "GitInfo")
                    {
                        elem.Remove();
                        break;
                    }
                }
            }
        }

        public static void ProcessProjUsings(string projPath)
        {
            foreach (var cs in Directory.EnumerateFiles(Path.GetDirectoryName(projPath)!, "*.cs", SearchOption.AllDirectories))
            {
                var lines = File.ReadAllLines(cs);
                if (lines.Any(l =>
                {
                    if (l.StartsWith("using Mutagen.Bethesda")) return true;
                    if (l.StartsWith("namespace Mutagen.Bethesda")) return true;
                    if (l.Contains("FormLink")) return true;
                    if (l.Contains("ModKey")) return true;
                    return false;
                }))
                {
                    File.WriteAllLines(
                        cs,
                        "using Mutagen.Bethesda.Plugins.Records;".AsEnumerable()
                            .And("using Mutagen.Bethesda.Plugins;")
                            .And("using Mutagen.Bethesda.Plugins.Order;")
                            .And("using Mutagen.Bethesda.Plugins.Aspects;")
                            .And("using Mutagen.Bethesda.Plugins.Cache;")
                            .And("using Mutagen.Bethesda.Plugins.Exceptions;")
                            .And("using Mutagen.Bethesda.Plugins.Binary;")
                            .And("using Mutagen.Bethesda.Archives;")
                            .And("using Mutagen.Bethesda.Strings;")
                            .And(lines.Where(x => x != "using Mutagen.Bethesda.Bsa;")));
                }
            }
        }

        public static void SwapVersioning(XElement proj, string package, string version, SemanticVersion curVersion)
        {
            foreach (var group in proj.Elements("ItemGroup"))
            {
                foreach (var elem in group.Elements().ToArray())
                {
                    if (!elem.Name.LocalName.Equals("PackageReference")) continue;
                    if (!elem.TryGetAttribute("Include", out var libAttr)) continue;
                    if (!libAttr.Value.Equals(package, StringComparison.OrdinalIgnoreCase)) continue;
                    if (!elem.TryGetAttribute("Version", out var existingVerStr)) continue;
                    if (!SemanticVersion.TryParse(existingVerStr.Value, out var semVer))
                    {
                        if (System.Version.TryParse(existingVerStr.Value, out var vers))
                        {
                            semVer = new SemanticVersion(
                                vers.Major,
                                vers.Minor,
                                vers.Build);
                        }
                        else
                        {
                            continue;
                        }
                    }
                    if (!semVer.Equals(curVersion)) continue;
                    elem.SetAttributeValue("Version", version);
                }
            }
        }

        public static async Task<ConfigurationState<RunnerRepoInfo>> CheckoutRunnerRepository(
            string proj,
            string localRepoDir,
            GitPatcherVersioning patcherVersioning,
            NugetVersioningTarget nugetVersioning,
            Action<string>? logger,
            CancellationToken cancel,
            bool compile = true)
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
                bool fetchIfMissing = patcherVersioning.Versioning switch
                {
                    PatcherVersioningEnum.Commit => true,
                    _ => false
                };
                switch (patcherVersioning.Versioning)
                {
                    case PatcherVersioningEnum.Tag:
                        if (string.IsNullOrWhiteSpace(patcherVersioning.Target)) return GetResponse<RunnerRepoInfo>.Fail("No tag selected");
                        repo.Fetch();
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
                        repo.Fetch();
                        var targetBranch = repo.Branches[patcherVersioning.Target];
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
                if (commit == null)
                {
                    if (!fetchIfMissing)
                    {
                        return GetResponse<RunnerRepoInfo>.Fail("Could not locate commit with given sha");
                    }
                    repo.Fetch();
                    commit = repo.Lookup(objId, ObjectType.Commit) as Commit;
                    if (commit == null)
                    {
                        return GetResponse<RunnerRepoInfo>.Fail("Could not locate commit with given sha");
                    }
                }

                cancel.ThrowIfCancellationRequested();
                var slnPath = GitPatcherRun.GetPathToSolution(localRepoDir);
                if (slnPath == null) return GetResponse<RunnerRepoInfo>.Fail("Could not locate solution to run.");

                var foundProjSubPath = SolutionPatcherRun.AvailableProject(slnPath, proj);

                if (foundProjSubPath == null) return GetResponse<RunnerRepoInfo>.Fail($"Could not locate target project file: {proj}.");

                cancel.ThrowIfCancellationRequested();
                logger?.Invoke($"Checking out {targetSha}");
                repo.Reset(ResetMode.Hard, commit, new CheckoutOptions());

                var projPath = Path.Combine(localRepoDir, foundProjSubPath);

                cancel.ThrowIfCancellationRequested();
                logger?.Invoke($"Mutagen Nuget: {nugetVersioning.MutagenVersioning} {nugetVersioning.MutagenVersion}");
                logger?.Invoke($"Synthesis Nuget: {nugetVersioning.SynthesisVersioning} {nugetVersioning.SynthesisVersion}");
                GitPatcherRun.ModifyProject(
                    slnPath,
                    drivingProjSubPath: foundProjSubPath,
                    mutagenVersion: nugetVersioning.MutagenVersioning == NugetVersioningEnum.Match ? null : nugetVersioning.MutagenVersion,
                    listedMutagenVersion: out var listedMutagenVersion,
                    synthesisVersion: nugetVersioning.SynthesisVersioning == NugetVersioningEnum.Match ? null : nugetVersioning.SynthesisVersion,
                    listedSynthesisVersion: out var listedSynthesisVersion);

                var runInfo = new RunnerRepoInfo(
                    SolutionPath: slnPath,
                    ProjPath: projPath,
                    Target: target,
                    CommitMessage: commit.Message,
                    CommitDate: commit.Author.When.LocalDateTime,
                    ListedSynthesisVersion: listedSynthesisVersion,
                    ListedMutagenVersion: listedMutagenVersion,
                    TargetSynthesisVersion: nugetVersioning.SynthesisVersioning == NugetVersioningEnum.Match ? listedSynthesisVersion : nugetVersioning.SynthesisVersion,
                    TargetMutagenVersion: nugetVersioning.MutagenVersioning == NugetVersioningEnum.Match ? listedMutagenVersion : nugetVersioning.MutagenVersion);

                // Compile to help prep
                if (compile)
                {
                    var compileResp = await DotNetCommands.Compile(projPath, cancel, logger);
                    logger?.Invoke("Finished compiling");
                    if (compileResp.Failed) return compileResp.BubbleResult(runInfo);
                }

                return GetResponse<RunnerRepoInfo>.Succeed(runInfo);
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
