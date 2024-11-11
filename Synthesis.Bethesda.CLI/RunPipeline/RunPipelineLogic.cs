using System.IO.Abstractions;
using Autofac;
using Mutagen.Bethesda.Environments.DI;
using Mutagen.Bethesda.Plugins.Order.DI;
using Noggog;
using Synthesis.Bethesda.Execution.Commands;
using Synthesis.Bethesda.Execution.Modules;
using Synthesis.Bethesda.Execution.Profile;
using Synthesis.Bethesda.Execution.Utility;

namespace Synthesis.Bethesda.CLI.RunPipeline;

public class RunPipelineLogic
{
    private readonly IStartupTask[] _startups;
    private readonly RunPatcherPipeline _runPipeline;

    public RunPipelineLogic(
        IStartupTask[] startups,
        RunPatcherPipeline runPipeline)
    {
        _startups = startups;
        _runPipeline = runPipeline;
    }
    
    public static async Task<int> Run(RunPatcherPipelineCommand cmd, IFileSystem? fileSystem = null)
    {
        try
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule(
                new RunPipelineModule(cmd));
            if (fileSystem != null)
            {
                builder.RegisterInstance(fileSystem).AsImplementedInterfaces();
            }
            builder.RegisterInstance(new ProfileIdentifier(cmd.ProfileIdentifier)).AsImplementedInterfaces();
            if (cmd.DataFolderPath.HasValue)
            {
                builder.RegisterInstance(new DataDirectoryInjection(cmd.DataFolderPath.Value))
                    .AsImplementedInterfaces();
            }

            if (cmd.LoadOrderFilePath.HasValue)
            {
                builder.RegisterInstance(new PluginListingsPathInjection(cmd.LoadOrderFilePath.Value))
                    .AsImplementedInterfaces();
            }
            
            var container = builder.Build();

            await container
                .Resolve<RunPipelineLogic>()
                .RunInternal().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            System.Console.Error.WriteLine(ex);
            return -1;
        }
        return 0;
    }

    private async Task RunInternal()
    {
        _startups.ForEach(x => x.Start());
        await _runPipeline.Run(CancellationToken.None);
    }
}