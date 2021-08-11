using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
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
        public IGetPatcherRunners GetPatcherRunners { get; }
        public IExecuteRun ExecuteRun { get; }
        public RunPatcherPipelineInstructions Instructions { get; }

        public RunPatcherPipeline(
            IExecuteRun executeRun,
            IGetPatcherRunners getPatcherRunners,
            RunPatcherPipelineInstructions instructions)
        {
            GetPatcherRunners = getPatcherRunners;
            ExecuteRun = executeRun;
            Instructions = instructions;
        }
        
        public async Task Run(CancellationToken cancel)
        {
            await ExecuteRun
                .Run(
                    outputPath: Instructions.OutputPath,
                    patchers: GetPatcherRunners.Get(),
                    sourcePath: Instructions.SourcePath,
                    cancel: cancel,
                    persistenceMode: Instructions.PersistenceMode ?? PersistenceMode.None,
                    persistencePath: Instructions.PersistencePath);
        }
    }
}