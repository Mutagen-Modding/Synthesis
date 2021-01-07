using CommandLine;
using Mutagen.Bethesda.Synthesis;
using Synthesis.Bethesda.Execution;
using Synthesis.Bethesda.Execution.CLI;
using Synthesis.Bethesda.Execution.Reporters;
using System;
using System.Threading;
using System.Threading.Tasks;
using Wabbajack.Common;

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
                                settings.DataFolderPath = settings.GameRelease.ToWjGame().MetaData().GameLocation().ToString();
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
