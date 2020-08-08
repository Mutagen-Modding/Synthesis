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
        public static async Task Run(
            string workingDirectory,
            ModPath outputPath,
            string dataFolder,
            IEnumerable<ModKey> loadOrder,
            GameRelease release,
            IEnumerable<IPatcherRun> patchers,
            ModPath? sourcePath = null,
            CancellationToken? cancellation = null,
            IRunReporter? reporter = null)
        {
            reporter ??= ThrowReporter.Instance;
            try
            {
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

                var patchersList = patchers.ToList();
                if (patchersList.Count == 0) return;

                bool problem = false;

                // Copy plugins text to working directory
                string loadOrderPath = Path.Combine(workingDirectory, "Plugins.txt");
                var writeLoadOrder = Task.Run(async () =>
                {
                    try
                    {
                        await File.WriteAllLinesAsync(
                            loadOrderPath,
                            loadOrder.Select(modKey => modKey.FileName));
                    }
                    catch (Exception ex)
                    {
                        reporter.ReportOverallProblem(ex);
                        problem = true;
                    }
                });

                // Prep all patchers in parallel
                var patcherPreps = patchersList.Select(patcher => Task.Run(async () =>
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
                }));

                await Task.WhenAll(patcherPreps.And(writeLoadOrder));
                if (problem) return;

                var prevPath = sourcePath;
                for (int i = 0; i < patchersList.Count; i++)
                {
                    var patcher = patchersList[i];
                    var nextPath = new ModPath(outputPath.ModKey, Path.Combine(workingDirectory, $"{i} - {patcher.Name}"));
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
