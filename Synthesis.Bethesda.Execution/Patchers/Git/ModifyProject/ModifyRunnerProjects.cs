using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis.Versioning;
using Noggog;
using NuGet.Versioning;
using Synthesis.Bethesda.Execution.Patchers.Solution;

namespace Synthesis.Bethesda.Execution.Patchers.Git.ModifyProject
{
    public interface IModifyRunnerProjects
    {
        void Modify(
            string solutionPath,
            string drivingProjSubPath,
            string? mutagenVersion,
            out string? listedMutagenVersion,
            string? synthesisVersion,
            out string? listedSynthesisVersion);
    }

    public class ModifyRunnerProjects : IModifyRunnerProjects
    {
        public readonly static System.Version NewtonSoftRemoveMutaVersion = new(0, 28);
        public readonly static System.Version NewtonSoftRemoveSynthVersion = new(0, 17, 5);
        public readonly static System.Version NamespaceMutaVersion = new(0, 30, 0);
        private readonly IAvailableProjectsRetriever _availableProjectsRetriever;
        private readonly ISwapOffNetCore _swapOffNetCore;
        private readonly IRemoveGitInfo _removeGitInfo;
        private readonly IAddNewtonsoftToOldSetups _addNewtonsoftToOldSetups;
        private readonly ISwapInDesiredVersionsForProjectString _swapDesiredVersions;
        private readonly ITurnOffWindowsSpecificationInTargetFramework _turnOffWindowsSpec;
        private readonly ISwapVersioning _swapVersioning;
        private readonly ITurnOffNullability _turnOffNullability;
        private readonly IProcessProjUsings _processProjUsings;
        private readonly IRemoveProject _removeProject;

        public ModifyRunnerProjects(
            IAvailableProjectsRetriever availableProjectsRetriever,
            ISwapOffNetCore swapOffNetCore,
            IRemoveGitInfo removeGitInfo,
            IAddNewtonsoftToOldSetups addNewtonsoftToOldSetups,
            ISwapInDesiredVersionsForProjectString swapDesiredVersions,
            ITurnOffWindowsSpecificationInTargetFramework turnOffWindowsSpec,
            ISwapVersioning swapVersioning,
            ITurnOffNullability turnOffNullability,
            IProcessProjUsings processProjUsings,
            IRemoveProject removeProject)
        {
            _availableProjectsRetriever = availableProjectsRetriever;
            _swapOffNetCore = swapOffNetCore;
            _removeGitInfo = removeGitInfo;
            _addNewtonsoftToOldSetups = addNewtonsoftToOldSetups;
            _swapDesiredVersions = swapDesiredVersions;
            _turnOffWindowsSpec = turnOffWindowsSpec;
            _swapVersioning = swapVersioning;
            _turnOffNullability = turnOffNullability;
            _processProjUsings = processProjUsings;
            _removeProject = removeProject;
        }
        
        public void Modify(
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
            foreach (var subProj in _availableProjectsRetriever.Get(solutionPath))
            {
                var proj = Path.Combine(Path.GetDirectoryName(solutionPath)!, subProj);
                var projXml = XElement.Parse(File.ReadAllText(proj));
                _swapDesiredVersions.Swap(
                    projXml,
                    mutagenVersion: mutagenVersion,
                    listedMutagenVersion: out var curListedMutagenVersion,
                    synthesisVersion: synthesisVersion,
                    listedSynthesisVersion: out var curListedSynthesisVersion);
                _turnOffNullability.TurnOff(projXml);
                _removeGitInfo.Remove(projXml);
                _swapOffNetCore.Swap(projXml);
                _turnOffWindowsSpec.TurnOff(projXml);
                System.Version.TryParse(curListedMutagenVersion, out var mutaVersion);
                System.Version.TryParse(curListedSynthesisVersion, out var synthVersion);
                _addNewtonsoftToOldSetups.Add(projXml, mutaVersion, synthVersion);
                System.Version.TryParse(trimmedMutagenVersion, out var targetMutaVersion);
                System.Version.TryParse(trimmedsynthesisVersion, out var targetSynthesisVersion);
                if ((targetMutaVersion != null
                    && targetMutaVersion >= NewtonSoftRemoveMutaVersion)
                    || (targetSynthesisVersion != null
                        && targetSynthesisVersion >= NewtonSoftRemoveSynthVersion))
                {
                    _removeProject.Remove(projXml, "Newtonsoft.Json");
                }

                if (targetMutaVersion >= NamespaceMutaVersion
                    && mutaVersion < NamespaceMutaVersion)
                {
                    _processProjUsings.Process(proj);
                    _swapVersioning.Swap(projXml, "Mutagen.Bethesda.FormKeys.SkyrimSE", "2.1", new SemanticVersion(2, 0, 0));
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
                _turnOffNullability.TurnOff(projXml);
                File.WriteAllText(item, projXml.ToString());
            }
        }
    }
}