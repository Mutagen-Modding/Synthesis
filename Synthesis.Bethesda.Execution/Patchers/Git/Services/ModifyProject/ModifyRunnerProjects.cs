using System.IO.Abstractions;
using System.Xml.Linq;
using Noggog;
using NuGet.Versioning;
using Serilog;
using Synthesis.Bethesda.Execution.Patchers.Solution;
using Synthesis.Bethesda.Execution.Versioning;

namespace Synthesis.Bethesda.Execution.Patchers.Git.Services.ModifyProject;

public interface IModifyRunnerProjects
{
    void Modify(
        FilePath solutionPath,
        string drivingProjSubPath,
        NugetVersionPair versions,
        out NugetVersionPair listedVersions);
}

public class ModifyRunnerProjects : IModifyRunnerProjects
{
    public static readonly SemanticVersion NewtonSoftRemoveMutaVersion = new(0, 28, 0);
    public static readonly SemanticVersion NewtonSoftRemoveSynthVersion = new(0, 17, 5);
    public static readonly SemanticVersion NamespaceMutaVersion = new(0, 30, 0);
    private readonly ILogger _logger;
    private readonly IFileSystem _fileSystem;
    private readonly IAvailableProjectsRetriever _availableProjectsRetriever;
    private readonly ISwapToProperNetVersion _swapToProperNetVersion;
    private readonly IRemoveGitInfo _removeGitInfo;
    private readonly IAddNewtonsoftToOldSetups _addNewtonsoftToOldSetups;
    private readonly ISwapInDesiredVersionsForProjectString _swapDesiredVersions;
    private readonly ITurnOffWindowsSpecificationInTargetFramework _turnOffWindowsSpec;
    private readonly ISwapVersioning _swapVersioning;
    private readonly ITurnOffNullability _turnOffNullability;
    private readonly IProcessProjUsings _processProjUsings;
    private readonly IRemoveProject _removeProject;
    private readonly AddAllReleasesToOldVersions _addAllReleasesToOldVersions;

    public ModifyRunnerProjects(
        ILogger logger,
        IFileSystem fileSystem,
        IAvailableProjectsRetriever availableProjectsRetriever,
        ISwapToProperNetVersion swapToProperNetVersion,
        IRemoveGitInfo removeGitInfo,
        IAddNewtonsoftToOldSetups addNewtonsoftToOldSetups,
        ISwapInDesiredVersionsForProjectString swapDesiredVersions,
        ITurnOffWindowsSpecificationInTargetFramework turnOffWindowsSpec,
        ISwapVersioning swapVersioning,
        ITurnOffNullability turnOffNullability,
        IProcessProjUsings processProjUsings,
        IRemoveProject removeProject,
        AddAllReleasesToOldVersions addAllReleasesToOldVersions)
    {
        _logger = logger;
        _fileSystem = fileSystem;
        _availableProjectsRetriever = availableProjectsRetriever;
        _swapToProperNetVersion = swapToProperNetVersion;
        _removeGitInfo = removeGitInfo;
        _addNewtonsoftToOldSetups = addNewtonsoftToOldSetups;
        _swapDesiredVersions = swapDesiredVersions;
        _turnOffWindowsSpec = turnOffWindowsSpec;
        _swapVersioning = swapVersioning;
        _turnOffNullability = turnOffNullability;
        _processProjUsings = processProjUsings;
        _removeProject = removeProject;
        _addAllReleasesToOldVersions = addAllReleasesToOldVersions;
    }

    string? TrimVersion(string? version, out string? prereleaseLabel)
    {
        if (version == null)
        {
            prereleaseLabel = null;
            return null;
        }
        var index = version.IndexOf('-');
        if (index == -1)
        {
            prereleaseLabel = null;
            return version;
        }
        
        prereleaseLabel = version.Substring(index + 1);
        return version.Substring(0, index);
    }

    private SemanticVersion? SemanticVersionParse(string? str)
    {
        if (str == null) return null;
        if (SemanticVersion.TryParse(str, out var semVer))
        {
            return semVer;
        }

        var trimmed = TrimVersion(str, out var prereleaseLabel);
        
        if (Version.TryParse(trimmed, out var vers))
        {
            if (prereleaseLabel == null)
            {
                return new SemanticVersion(vers.Major, vers.Minor, vers.Build == -1 ? 0 : vers.Build);
            }
            return new SemanticVersion(vers.Major, vers.Minor, vers.Build == -1 ? 0 : vers.Build, prereleaseLabel);
        }

        return null;
    }
        
    public void Modify(
        FilePath solutionPath,
        string drivingProjSubPath,
        NugetVersionPair versions,
        out NugetVersionPair listedVersions)
    {
        listedVersions = new NugetVersionPair(null, null);

        foreach (var subProj in _availableProjectsRetriever.Get(solutionPath))
        {
            var proj = Path.Combine(Path.GetDirectoryName(solutionPath)!, subProj);
            _logger.Information("Modifying {ProjPath}", proj);
            var txt = _fileSystem.File.ReadAllText(proj);
            var projXml = XElement.Parse(txt);
            _swapDesiredVersions.Swap(
                projXml,
                versions,
                out var curListedVersions);
            _turnOffNullability.TurnOff(projXml);
            _removeGitInfo.Remove(projXml);
            _turnOffWindowsSpec.TurnOff(projXml);
            var mutaVersion = SemanticVersionParse(curListedVersions.Mutagen);
            var synthVersion = SemanticVersionParse(curListedVersions.Synthesis);
            _addNewtonsoftToOldSetups.Add(projXml, mutaVersion, synthVersion);
            var targetMutaVersion = SemanticVersionParse(versions.Mutagen);
            var targetSynthesisVersion = SemanticVersionParse(versions.Synthesis);
            if ((targetMutaVersion != null
                 && targetMutaVersion >= NewtonSoftRemoveMutaVersion)
                || (targetSynthesisVersion != null
                    && targetSynthesisVersion >= NewtonSoftRemoveSynthVersion))
            {
                _removeProject.Remove(projXml, "Newtonsoft.Json");
            }

            if (targetMutaVersion != null)
            {
                _swapToProperNetVersion.Swap(projXml, targetMutaVersion);
            }

            if (targetMutaVersion != null && targetSynthesisVersion != null)
            {
                _addAllReleasesToOldVersions.Add(projXml, synthVersion, targetMutaVersion, targetSynthesisVersion);
            }

            if (targetMutaVersion != null
                && mutaVersion != null
                && targetMutaVersion >= NamespaceMutaVersion
                && mutaVersion < NamespaceMutaVersion)
            {
                _processProjUsings.Process(proj);
                _swapVersioning.Swap(projXml, "Mutagen.Bethesda.FormKeys.SkyrimSE", "2.1", new SemanticVersion(2, 0, 0));
            }

            var outputStr = projXml.ToString();
            if (!txt.Equals(outputStr))
            {
                _fileSystem.File.WriteAllText(proj, outputStr);
            }

            if (drivingProjSubPath.Equals(subProj))
            {
                listedVersions = curListedVersions;
            }
        }
        foreach (var item in Directory.EnumerateFiles(Path.GetDirectoryName(solutionPath)!, "Directory.Build.*"))
        {
            var txt = _fileSystem.File.ReadAllText(item);
            var projXml = XElement.Parse(txt);
            _turnOffNullability.TurnOff(projXml);
            var outputStr = projXml.ToString();
            if (!txt.Equals(outputStr))
            {
                _fileSystem.File.WriteAllText(item, outputStr);
            }
        }
    }
}