using System.IO.Abstractions;
using Autofac;
using Noggog;
using Synthesis.Bethesda.Execution.Commands;
using Synthesis.Bethesda.Execution.Exceptions;

namespace Synthesis.Bethesda.CLI.RunPipeline;

public class RunPipelineLogic
{
    private readonly RunPatcherPipeline _runPipeline;

    public RunPipelineLogic(
        RunPatcherPipeline runPipeline)
    {
        _runPipeline = runPipeline;
    }
    
    public static async Task<int> Run(RunPatcherPipelineCommand cmd, IFileSystem? fileSystem = null)
    {
        var builder = new ContainerBuilder();
        builder.RegisterModule(
            new RunPipelineModule(fileSystem.GetOrDefault(), cmd));
            
        var container = builder.Build();

        try
        {
            await container
                .Resolve<RunPipelineLogic>()
                .RunInternal().ConfigureAwait(false);
        }
        catch (ClassifiedErrorException)
        {
            return -1;
        }
        return 0;
    }

    private async Task RunInternal()
    {
        await _runPipeline.Run(CancellationToken.None);
    }
}