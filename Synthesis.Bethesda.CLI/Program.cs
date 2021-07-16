using CommandLine;
using System;
using System.IO;
using System.Threading.Tasks;
using Autofac;
using Mutagen.Bethesda.Installs;
using Synthesis.Bethesda.Execution.Running;

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
                            // Locate data folder
                            if (string.IsNullOrWhiteSpace(settings.DataFolderPath))
                            {
                                if (!GameLocations.TryGetGameFolder(settings.GameRelease, out var gameFolder))
                                {
                                    throw new DirectoryNotFoundException("Could not find game folder automatically");
                                }
                                settings.DataFolderPath = Path.Combine(gameFolder, "Data");
                            }

                            var builder = new ContainerBuilder();
                            builder.RegisterModule(
                                new MainModule(settings));
                            await builder.Build()
                                .Resolve<IRunPatcherPipeline>()
                                .Run(settings);
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
