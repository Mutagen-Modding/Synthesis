using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Noggog;
using Synthesis.Bethesda.Execution.Commands;
using Synthesis.Bethesda.Execution.Utility;

namespace Synthesis.Bethesda.CLI.RunPipeline;

public class RunPipelineLogic
{
    private readonly IStartupTask[] _startups;
    private readonly IRunPatcherPipeline _runPipeline;

    public RunPipelineLogic(
        IStartupTask[] startups,
        IRunPatcherPipeline runPipeline)
    {
        _startups = startups;
        _runPipeline = runPipeline;
    }
    
    public static async Task<int> Run(RunPatcherPipelineInstructions settings)
    {
        try
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule(
                new RunPipelineModule(settings));
            var container = builder.Build();

            var profile = container.Resolve<IRunProfileProvider>();

            using var runScope = container.BeginLifetimeScope(c =>
            {
                c.RegisterType<RunPipelineLogic>().AsSelf();
                c.RegisterInstance(profile.Get())
                    .AsImplementedInterfaces();
            });
                            
            await runScope
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