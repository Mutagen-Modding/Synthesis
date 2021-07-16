using CommandLine;
using Synthesis.Bethesda.Execution.CLI;
using Synthesis.Bethesda.Execution.Reporters;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Mutagen.Bethesda.Environments.DI;
using Mutagen.Bethesda.Installs;
using Mutagen.Bethesda.Plugins.Order.DI;
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
                            builder.RegisterModule<MainModule>();
                            builder.RegisterInstance(new GameReleaseInjection(settings.GameRelease))
                                .As<IGameReleaseContext>();
                            builder.RegisterInstance(new DataDirectoryInjection(settings.DataFolderPath))
                                .As<IDataDirectoryProvider>();
                            builder.RegisterInstance(new PluginListingsPathInjection(settings.LoadOrderFilePath))
                                .As<IPluginListingsPathProvider>();
                            await builder.Build().Resolve<IRunPatcherPipeline>()
                                .Run(settings, CancellationToken.None, new ConsoleReporter());
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
