using CommandLine;
using Synthesis.Bethesda.Execution.CLI;
using Synthesis.Bethesda.Execution.Reporters;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Mutagen.Bethesda.Installs;

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
                            await Commands.Run(settings, CancellationToken.None, new ConsoleReporter());
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
