using CommandLine;
using Synthesis.Bethesda.Execution.CLI;
using Synthesis.Bethesda.Execution.Reporters;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Mutagen.Bethesda.Environments.DI;
using Mutagen.Bethesda.Installs;
using Mutagen.Bethesda.Plugins.Order.DI;

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
                            Inject.Instance.Configure(cfg =>
                            {
                                cfg.For<IGameReleaseContext>().Use(new GameReleaseInjection(settings.GameRelease));
                                cfg.For<IDataDirectoryProvider>().Use(new DataDirectoryInjection(settings.DataFolderPath));
                                cfg.For<IPluginListingsPathProvider>().Use(new PluginListingsPathInjection(settings.LoadOrderFilePath));
                            });
                            await Inject.Instance.GetInstance<IRunPatcherPipeline>()
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
