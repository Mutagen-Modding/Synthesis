using System.IO.Abstractions;
using CommandLine;
using Synthesis.Bethesda.CLI.CreateNewPatcher;
using Synthesis.Bethesda.CLI.RunPipeline;
using Synthesis.Bethesda.Execution.Commands;

namespace Synthesis.Bethesda.CLI;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var fs = new FileSystem();
        return await Parser.Default.ParseArguments(args, 
                typeof(RunPatcherPipelineCommand), 
                typeof(CreatePatcherCommand))
            .MapResult(
                async (RunPatcherPipelineCommand cmd) => await RunPipelineLogic.Run(cmd),
                async (CreatePatcherCommand cmd) => await new CreateTemplatePatcherSolutionRunner(fs).Run(cmd),
                async _ => -1);
    }
}