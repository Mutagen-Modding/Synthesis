using System.IO.Abstractions;
using CommandLine;
using Synthesis.Bethesda.CLI.AddGitPatcher;
using Synthesis.Bethesda.CLI.AddSolutionPatcher;
using Synthesis.Bethesda.CLI.CreateProfileCli;
using Synthesis.Bethesda.CLI.CreateTemplatePatcher;
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
                typeof(CreateProfileCommand), 
                typeof(AddGitPatcherCommand), 
                typeof(AddSolutionPatcherCommand), 
                typeof(CreateTemplatePatcherCommand))
            .MapResult(
                async (RunPatcherPipelineCommand cmd) => await RunPipelineLogic.Run(cmd),
                async (CreateTemplatePatcherCommand cmd) => await new CreateTemplatePatcherSolutionRunner(fs).Run(cmd),
                async (AddGitPatcherCommand cmd) => await AddGitPatcherRunner.Run(cmd),
                async (AddSolutionPatcherCommand cmd) => await AddSolutionPatcherRunner.Run(cmd),
                async (CreateProfileCommand cmd) => await CreateProfileRunner.Run(cmd),
                async _ => -1);
    }
}