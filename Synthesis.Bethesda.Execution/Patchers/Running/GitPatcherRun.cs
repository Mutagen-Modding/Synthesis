using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis.Versioning;
using Noggog;
using NuGet.Versioning;
using Synthesis.Bethesda.Execution.GitRespository;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Settings;

namespace Synthesis.Bethesda.Execution.Patchers.Running
{
    public interface IGitPatcherRun : IPatcherRun
    {
    }

    public class GitPatcherRun : IGitPatcherRun
    {
        public readonly static System.Version NewtonSoftAddMutaVersion = new(0, 26);
        public readonly static System.Version NewtonSoftAddSynthVersion = new(0, 14, 1);
        public readonly static System.Version NewtonSoftRemoveMutaVersion = new(0, 28);
        public readonly static System.Version NewtonSoftRemoveSynthVersion = new(0, 17, 5);
        public readonly static System.Version NamespaceMutaVersion = new(0, 30, 0);
        public string Name { get; }
        private readonly GithubPatcherSettings _settings;
        private readonly IRunnerRepoDirectoryProvider _runnerRepoDirectoryProvider;
        private readonly ICheckOrCloneRepo _CheckOrClone;
        public SolutionPatcherRun? SolutionRun { get; private set; }
        private readonly CompositeDisposable _disposable = new();

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

        public delegate IGitPatcherRun Factory(GithubPatcherSettings settings);
        
        public GitPatcherRun(
            GithubPatcherSettings settings,
            IRunnerRepoDirectoryProvider runnerRepoDirectoryProvider,
            ICheckOrCloneRepo checkOrClone)
        {
            _settings = settings;
            _runnerRepoDirectoryProvider = runnerRepoDirectoryProvider;
            _CheckOrClone = checkOrClone;
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

        public async Task Prep(GameRelease release, CancellationToken cancel)
        {
            _output.OnNext("Cloning repository");
            var cloneResult = _CheckOrClone.Check(
                GetResponse<string>.Succeed(_settings.RemoteRepoPath),
                _runnerRepoDirectoryProvider.Path, 
                (x) => _output.OnNext(x),
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
            using var outputSub = SolutionRun.Output.Subscribe(this._output);
            using var errSub = SolutionRun.Error.Subscribe(this._error);
            await SolutionRun.Run(settings, cancel).ConfigureAwait(false);
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
                    SwapVersioning(projXml, "Mutagen.Bethesda.FormKeys.SkyrimSE", "2.1", new SemanticVersion(2, 0, 0));
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

    }
}
