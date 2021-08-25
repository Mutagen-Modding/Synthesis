using System.Threading;
using System.Threading.Tasks;
using Synthesis.Bethesda.Execution.Commands;
using Synthesis.Bethesda.Execution.Running.Cli.Settings;
using Synthesis.Bethesda.Execution.Running.Runner;
using Synthesis.Bethesda.Execution.Settings;

namespace Synthesis.Bethesda.Execution.Running.Cli
{
    public interface IRunPatcherPipeline
    {
        Task Run(CancellationToken cancel);
    }

    public class RunPatcherPipeline : IRunPatcherPipeline
    {
        public IExecuteRun ExecuteRun { get; }
        public IGetGroupRunners GetGroupRunners { get; }
        public RunPatcherPipelineInstructions Instructions { get; }

        public RunPatcherPipeline(
            IExecuteRun executeRun,
            IGetGroupRunners getGroupRunners,
            RunPatcherPipelineInstructions instructions)
        {
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
                    persistenceMode: Instructions.PersistenceMode ?? PersistenceMode.None,
                    persistencePath: Instructions.PersistencePath);
        }
    }
}