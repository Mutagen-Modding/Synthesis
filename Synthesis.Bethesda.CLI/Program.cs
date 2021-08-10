using CommandLine;
using System;
using System.IO;
using System.Threading.Tasks;
using Autofac;
using Mutagen.Bethesda.Installs;
using Synthesis.Bethesda.Execution.Running;
using Synthesis.Bethesda.Execution.Running.Cli;
using Synthesis.Bethesda.Execution.Running.Cli.Settings;

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
                                c.RegisterInstance(profile.Profile)
                                    .AsImplementedInterfaces();
                            });
                            
                            await runScope
                                .Resolve<IRunPatcherPipeline>()
                                .Run();
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
                    });
        }
    }
}
