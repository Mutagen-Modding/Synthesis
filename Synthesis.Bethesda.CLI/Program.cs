using CommandLine;
using Synthesis.Bethesda.CLI.RunPipeline;
using Synthesis.Bethesda.Execution.Commands;

namespace Synthesis.Bethesda.CLI;

class Program
{
    static async Task<int> Main(string[] args)
    {
        return await Parser.Default.ParseArguments(args, typeof(RunPatcherPipelineCommand))
            .MapResult(
                async (RunPatcherPipelineCommand cmd) => await RunPipelineLogic.Run(cmd),
                async _ => -1);
    }
}