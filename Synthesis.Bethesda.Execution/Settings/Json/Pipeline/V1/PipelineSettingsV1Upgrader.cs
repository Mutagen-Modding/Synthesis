using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using Noggog;
using Serilog;
using Synthesis.Bethesda.Execution.Pathing;
using Synthesis.Bethesda.Execution.Settings.V2;
using Vers1 = Synthesis.Bethesda.Execution.Settings.V1.PipelineSettings;
using Vers2 = Synthesis.Bethesda.Execution.Settings.V2.PipelineSettings;

namespace Synthesis.Bethesda.Execution.Settings.Json.Pipeline.V1
{
    public interface IPipelineSettingsV1Upgrader
    {
        Vers2 Upgrade(Vers1 input);
    }

    public class PipelineSettingsV1Upgrader : IPipelineSettingsV1Upgrader
    {
        private readonly IFileSystem _fileSystem;
        private readonly IExtraDataPathProvider _extraDataPathProvider;

        public PipelineSettingsV1Upgrader(
            IFileSystem fileSystem,
            IExtraDataPathProvider extraDataPathProvider)
        {
            _fileSystem = fileSystem;
            _extraDataPathProvider = extraDataPathProvider;
        }
        
        public Vers2 Upgrade(Vers1 input)
        {
            MoveUserData(input);
            return UpgradeSettingsObject(input);
        }

        private void MoveUserData(Vers1 input)
        {
            Log.Logger.Information("Migrating user data to v2");
            if (!_fileSystem.Directory.Exists(_extraDataPathProvider.Path))
            {
                Log.Logger.Information("No user data to migrate");
                return;
            }
            
            Dictionary<string, Synthesis.Bethesda.Execution.Settings.V1.SynthesisProfile> patcherNamesToProfile = new();
            foreach (var synthesisProfile in input.Profiles)
            {
                foreach (var patcher in synthesisProfile.Patchers)
                {
                    patcherNamesToProfile.GetOrAdd(patcher.Nickname, () => synthesisProfile);
                }
            }

            foreach (var patcherSettingsDir in _fileSystem.Directory.EnumerateDirectoryPaths(_extraDataPathProvider.Path, includeSelf: false, recursive: false))
            {
                var existingFolder = Path.Combine(_extraDataPathProvider.Path, patcherSettingsDir.Name);
                if (!_fileSystem.Directory.Exists(existingFolder)) continue;
                string newFolder;
                if (patcherNamesToProfile.TryGetValue(patcherSettingsDir.Name, out var profile))
                {
                    newFolder = Path.Combine(_extraDataPathProvider.Path, profile.Nickname, patcherSettingsDir.Name);
                }
                else
                {
                    newFolder = Path.Combine(_extraDataPathProvider.Path, "Unknown Profile", patcherSettingsDir.Name);
                }
                if (_fileSystem.Directory.Exists(newFolder)) continue;
                _fileSystem.Directory.CreateDirectory(Path.GetDirectoryName(newFolder));
                _fileSystem.Directory.Move(existingFolder, newFolder);
            }
        }

        private Vers2 UpgradeSettingsObject(Vers1 input)
        {
            return new Vers2()
            {
                Profiles = input.Profiles.Select(x => (ISynthesisProfileSettings)new SynthesisProfile()
                {
                    Nickname = x.Nickname,
                    ID = x.ID,
                    TargetRelease = x.TargetRelease,
                    MutagenVersioning = x.MutagenVersioning,
                    MutagenManualVersion = x.MutagenManualVersion,
                    SynthesisVersioning = x.SynthesisVersioning,
                    SynthesisManualVersion = x.SynthesisManualVersion,
                    DataPathOverride = x.DataPathOverride,
                    ConsiderPrereleaseNugets = x.ConsiderPrereleaseNugets,
                    LockToCurrentVersioning = x.LockToCurrentVersioning,
                    Persistence = x.Persistence,
                    IgnoreMissingMods = x.IgnoreMissingMods,
                    Groups = new List<PatcherGroupSettings>()
                    {
                        new PatcherGroupSettings()
                        {
                            Name = Synthesis.Bethesda.Constants.SynthesisName,
                            On = true,
                            Expanded = true,
                            Patchers = x.Patchers
                        }
                    },
                }).ToList()
            };
        }
    }
}