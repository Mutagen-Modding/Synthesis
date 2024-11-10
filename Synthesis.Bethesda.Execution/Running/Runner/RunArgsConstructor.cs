using Mutagen.Bethesda.Environments.DI;
using Noggog;
using Synthesis.Bethesda.Commands;
using Synthesis.Bethesda.Execution.Groups;
using Synthesis.Bethesda.Execution.Patchers.Running;
using Synthesis.Bethesda.Execution.Patchers.TopLevel;
using Synthesis.Bethesda.Execution.Profile;
using Synthesis.Bethesda.Execution.Profile.Services;
using Synthesis.Bethesda.Execution.Settings;

namespace Synthesis.Bethesda.Execution.Running.Runner;

public interface IRunArgsConstructor
{
    RunSynthesisPatcher GetArgs(
        IGroupRun groupRun,
        IPatcherRun patcher,
        FilePath? sourcePath,
        RunParameters runParameters);
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
        IGroupRun groupRun,
        IPatcherRun patcher,
        FilePath? sourcePath,
        RunParameters runParameters)
    {
        var fileName = PatcherNameSanitizer.Sanitize(patcher.Name);

        if (fileName.IsNullOrWhitespace())
        {
            throw new ArgumentNullException("Sanitized patcher name was null or whitespace: {patcher.Name}");
        }
        
        var nextPath = new FilePath(
            Path.Combine(ProfileDirectories.WorkingDirectory, groupRun.ModKey.Name, $"{patcher.Index} - {fileName}", groupRun.ModKey.FileName));

        return new RunSynthesisPatcher()
        {
            SourcePath = sourcePath,
            OutputPath = nextPath,
            DataFolderPath = DataDirectoryProvider.Path,
            GameRelease = ReleaseContext.Release,
            Localize = runParameters.Localize,
            TargetLanguage = runParameters.TargetLanguage.ToString(),
            LoadOrderFilePath = RunLoadOrderPathProvider.PathFor(groupRun),
            PersistencePath = runParameters.PersistenceMode == PersistenceMode.None ? null : runParameters.PersistencePath,
            PatcherName = fileName,
            ModKey = groupRun.ModKey.FileName,
            UseUtf8ForEmbeddedStrings = runParameters.UseUtf8ForEmbeddedStrings,
            HeaderVersionOverride = runParameters.HeaderVersionOverride,
            FormIDRangeMode = runParameters.FormIDRangeMode,
        };
    }
}