using System;
using System.IO;
using Mutagen.Bethesda.Environments.DI;
using Mutagen.Bethesda.Plugins;
using Noggog;
using Synthesis.Bethesda.Commands;
using Synthesis.Bethesda.Execution.Patchers.Running;
using Synthesis.Bethesda.Execution.Patchers.TopLevel;
using Synthesis.Bethesda.Execution.Profile;

namespace Synthesis.Bethesda.Execution.Running.Runner
{
    public interface IRunArgsConstructor
    {
        RunSynthesisPatcher GetArgs(
            IPatcherRun patcher,
            ModKey outputKey,
            FilePath? sourcePath,
            string? persistencePath);
    }

    public class RunArgsConstructor : IRunArgsConstructor
    {
        public IGameReleaseContext ReleaseContext { get; }
        public IDataDirectoryProvider DataDirectoryProvider { get; }
        public IRunLoadOrderPathProvider RunLoadOrderPathProvider { get; }
        public IProfileDirectories ProfileDirectories { get; }
        public IPatcherNameSanitizer PatcherNameSanitizer { get; }

        public RunArgsConstructor(
            IPatcherNameSanitizer patcherNameSanitizer,
            IGameReleaseContext releaseContext,
            IDataDirectoryProvider dataDirectoryProvider,
            IRunLoadOrderPathProvider runLoadOrderPathProvider,
            IProfileDirectories profileDirectories)
        {
            ReleaseContext = releaseContext;
            DataDirectoryProvider = dataDirectoryProvider;
            RunLoadOrderPathProvider = runLoadOrderPathProvider;
            ProfileDirectories = profileDirectories;
            PatcherNameSanitizer = patcherNameSanitizer;
        }
        
        public RunSynthesisPatcher GetArgs(
            IPatcherRun patcher,
            ModKey outputKey,
            FilePath? sourcePath,
            string? persistencePath)
        {
            var fileName = PatcherNameSanitizer.Sanitize(patcher.Name);
            var nextPath = new FilePath(
                Path.Combine(ProfileDirectories.WorkingDirectory, $"{patcher.Index} - {fileName}"));

            return new RunSynthesisPatcher()
            {
                SourcePath = sourcePath,
                OutputPath = nextPath,
                DataFolderPath = DataDirectoryProvider.Path,
                GameRelease = ReleaseContext.Release,
                LoadOrderFilePath = RunLoadOrderPathProvider.Path,
                PersistencePath = persistencePath,
                PatcherName = fileName,
                ModKey = outputKey.FileName,
            };
        }
    }
}