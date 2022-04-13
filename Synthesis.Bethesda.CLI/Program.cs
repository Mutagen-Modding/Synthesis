using CommandLine;
using System;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Synthesis.Bethesda.CLI.RunPipeline;
using Synthesis.Bethesda.Execution.Commands;

namespace Synthesis.Bethesda.CLI;

class Program
{
    static async Task<int> Main(string[] args)
    {
        return await Parser.Default.ParseArguments(args, typeof(RunPatcherPipelineInstructions))
            .MapResult(
                async (RunPatcherPipelineInstructions settings) => await RunPipelineLogic.Run(settings),
                async _ => -1);
    }
}