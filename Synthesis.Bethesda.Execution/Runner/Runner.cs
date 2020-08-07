using Synthesis.Bethesda.Execution.Patchers;
using Noggog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Mutagen.Bethesda;

namespace Synthesis.Bethesda.Execution.Runner
{
    public class Runner
    {
        public readonly static ModKey TypicalModKey = new ModKey("Synthesis", ModType.Plugin);

        public static async Task Run(
            string workingDirectory,
            ModPath outputPath,
            string dataFolder,
            GameRelease release,
            IReadOnlyList<IPatcherRun> patchers,
            ModKey? modKeyOverride = null,
            ModPath? sourcePath = null,
            CancellationToken? cancellation = null,
            IRunReporter? reporter = null)
        {
            reporter ??= ThrowReporter.Instance;
            try
            {
                modKeyOverride ??= TypicalModKey;
                if (sourcePath != null)
                {
                    if (!File.Exists(sourcePath))
                    {
                        reporter.ReportOverallProblem(new FileNotFoundException($"Source path did not exist: {sourcePath}"));
                        return;
                    }
                }
                var dirInfo = new DirectoryInfo(workingDirectory);
                dirInfo.DeleteEntireFolder();
                dirInfo.Create();

                if (patchers.Count == 0) return;

                if (!LoadOrder.TryGetPluginsFile(release, out var path)
                    || !File.Exists(path.Path))
                {
                    reporter.ReportOverallProblem(new FileNotFoundException($"Could not locate load order file for: {release}"));
                    return;
                }
                // Copy plugins text to working directory
                string loadOrderPath = Path.Combine(workingDirectory, "Plugins.txt");
                File.Copy(path.Path, loadOrderPath);

                // Prep all patchers in parallel
                bool problem = false;
                await Task.WhenAll(patchers.Select(patcher => Task.Run(async () =>
                {
                    try
                    {
                        await patcher.Prep(release, cancellation);
                    }
                    catch (Exception ex)
                    {
                        reporter.ReportPrepProblem(patcher, ex);
                        problem = true;
                    }
                })));
                if (problem) return;

                var prevPath = sourcePath;
                for (int i = 0; i < patchers.Count; i++)
                {
                    var patcher = patchers[i];
                    var nextPath = new ModPath(modKeyOverride.Value, Path.Combine(workingDirectory, $"{i} - {patcher.Name}"));
                    try
                    {
                        await patcher.Run(new RunSynthesisPatcher()
                        {
                            SourcePath = prevPath?.Path,
                            OutputPath = nextPath,
                            DataFolderPath = dataFolder,
                            GameRelease = release,
                            LoadOrderFilePath = loadOrderPath,
                        });
                    }
                    catch (Exception ex)
                    {
                        reporter.ReportRunProblem(patcher, ex);
                        return;
                    }
                    if (!File.Exists(nextPath))
                    {
                        reporter.ReportRunProblem(patcher, new ArgumentException($"Patcher {patcher.Name} did not produce output file."));
                        return;
                    }
                    reporter.ReportOutputMapping(patcher, nextPath);
                    prevPath = nextPath;
                }
                File.Copy(prevPath!.Path, outputPath);
            }
            catch (Exception ex)
            {
                reporter.ReportOverallProblem(ex);
            }
        }
    }
}
