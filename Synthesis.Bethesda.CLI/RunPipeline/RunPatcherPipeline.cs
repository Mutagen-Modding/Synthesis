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
    public RunPatcherPipelineInstructions Instructions { get; }

    public RunPatcherPipeline(
        IExecuteRun executeRun,
        IGetGroupRunners getGroupRunners,
        ISynthesisProfileSettings profileSettings,
        RunPatcherPipelineInstructions instructions)
    {
        ProfileSettings = profileSettings;
        ExecuteRun = executeRun;
        GetGroupRunners = getGroupRunners;
        Instructions = instructions;
    }
        
    public async Task Run(CancellationToken cancel)
    {
        await ExecuteRun
            .Run(
                groups: GetGroupRunners.Get(cancel),
                sourcePath: Instructions.SourcePath,
                outputDir: Instructions.OutputDirectory,
                cancel: cancel,
                runParameters: new RunParameters(
                    ProfileSettings.TargetLanguage,
                    ProfileSettings.Localize,
                    Instructions.PersistenceMode ?? PersistenceMode.None, 
                    Instructions.PersistencePath)).ConfigureAwait(false);
    }
}