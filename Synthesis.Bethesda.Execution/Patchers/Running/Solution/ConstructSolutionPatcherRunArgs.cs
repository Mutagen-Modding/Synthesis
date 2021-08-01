using System.IO.Abstractions;
using Mutagen.Bethesda.Synthesis.CLI;
using Synthesis.Bethesda.Execution.Patchers.Solution;
using Synthesis.Bethesda.Execution.Patchers.TopLevel;

namespace Synthesis.Bethesda.Execution.Patchers.Running.Solution
{
    public interface IConstructSolutionPatcherRunArgs
    {
        RunSynthesisMutagenPatcher Construct(RunSynthesisPatcher settings);
    }

    public class ConstructSolutionPatcherRunArgs : IConstructSolutionPatcherRunArgs
    {
        private readonly IFileSystem _fileSystem;
        public IPatcherExtraDataPathProvider PatcherExtraDataPathProvider { get; }
        public IDefaultDataPathProvider DefaultDataPathProvider { get; }

        public ConstructSolutionPatcherRunArgs(
            IFileSystem fileSystem,
            IPatcherExtraDataPathProvider patcherExtraDataPathProvider,
            IDefaultDataPathProvider defaultDataPathProvider)
        {
            _fileSystem = fileSystem;
            PatcherExtraDataPathProvider = patcherExtraDataPathProvider;
            DefaultDataPathProvider = defaultDataPathProvider;
        }
        
        public RunSynthesisMutagenPatcher Construct(RunSynthesisPatcher settings)
        {
            var defaultDataFolderPath = DefaultDataPathProvider.Path;

            return new RunSynthesisMutagenPatcher()
            {
                DataFolderPath = settings.DataFolderPath,
                ExtraDataFolder = PatcherExtraDataPathProvider.Path,
                DefaultDataFolderPath = _fileSystem.Directory.Exists(defaultDataFolderPath) ? defaultDataFolderPath.Path : null,
                GameRelease = settings.GameRelease,
                LoadOrderFilePath = settings.LoadOrderFilePath,
                OutputPath = settings.OutputPath,
                SourcePath = settings.SourcePath,
                PatcherName = settings.PatcherName,
                PersistencePath = settings.PersistencePath
            };
        }
    }
}