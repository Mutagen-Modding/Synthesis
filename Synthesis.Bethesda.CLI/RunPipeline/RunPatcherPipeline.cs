using Synthesis.Bethesda.Execution.Commands;
using Synthesis.Bethesda.Execution.Running.Runner;
using Synthesis.Bethesda.Execution.Settings;

namespace Synthesis.Bethesda.CLI.RunPipeline;

public interface IRunPatcherPipeline
{
    Task Run(CancellationToken cancel);
}

public class RunPatcherPipeline : IRunPatcherPipeline
{
    public ISynthesisProfileSettings ProfileSettings { get; }
    public IExecuteRun ExecuteRun { get; }
    public IGetGroupRunners GetGroupRunners { get; }
    public RunPatcherPipelineCommand Command { get; }

    public RunPatcherPipeline(
        IExecuteRun executeRun,
        IGetGroupRunners getGroupRunners,
        ISynthesisProfileSettings profileSettings,
        RunPatcherPipelineCommand command)
    {
        ProfileSettings = profileSettings;
        ExecuteRun = executeRun;
        GetGroupRunners = getGroupRunners;
        Command = command;
    }
        
    public async Task Run(CancellationToken cancel)
    {
        await ExecuteRun
            .Run(
                groups: GetGroupRunners.Get(cancel),
                outputDir: Command.OutputDirectory,
                cancel: cancel,
                runParameters: new RunParameters(
                    TargetLanguage: ProfileSettings.TargetLanguage,
                    Localize: ProfileSettings.Localize,
                    UseUtf8ForEmbeddedStrings: ProfileSettings.UseUtf8ForEmbeddedStrings,
                    HeaderVersionOverride: ProfileSettings.HeaderVersionOverride,
                    FormIDRangeMode: ProfileSettings.FormIDRangeMode,
                    PersistenceMode: Command.PersistenceMode ?? PersistenceMode.None, 
                    PersistencePath: Command.PersistencePath,
                    Master: ProfileSettings.ExportAsMasterFiles,
                    MasterStyleFallbackEnabled: ProfileSettings.MasterStyleFallbackEnabled,
                    MasterStyle: ProfileSettings.MasterStyle)).ConfigureAwait(false);
    }
}