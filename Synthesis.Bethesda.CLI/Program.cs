using CommandLine;
using System;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Synthesis.Bethesda.Execution.Commands;
using Synthesis.Bethesda.Execution.Running.Cli;

namespace Synthesis.Bethesda.CLI
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            return await Parser.Default.ParseArguments(args, typeof(RunPatcherPipelineInstructions))
                .MapResult(
                    async (RunPatcherPipelineInstructions settings) =>
                    {
                        try
                        {
                            var builder = new ContainerBuilder();
                            builder.RegisterModule(
                                new MainModule(settings));
                            var container = builder.Build();

                            var profile = container.Resolve<IRunProfileProvider>();

                            using var runScope = container.BeginLifetimeScope(c =>
                            {
                                c.RegisterInstance(profile.Get())
                                    .AsImplementedInterfaces();
                            });
                            
                            await runScope
                                .Resolve<IRunPatcherPipeline>()
                                .Run(CancellationToken.None).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            System.Console.Error.WriteLine(ex);
                            return -1;
                        }
                        return 0;
                    },
                    async _ =>
                    {
                        return -1;
                    }).ConfigureAwait(false);
        }
    }
}
