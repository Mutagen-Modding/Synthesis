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
        public static async Task<bool> Run(
            string workingDirectory,
            ModPath outputPath,
            string dataFolder,
            IEnumerable<LoadOrderListing> loadOrder,
            GameRelease release,
            IEnumerable<IPatcherRun> patchers,
            ModPath? sourcePath = null,
            CancellationToken? cancellation = null,
            IRunReporter? reporter = null)
        {
            return await Run<object?>(
                workingDirectory: workingDirectory,
                outputPath: outputPath,
                dataFolder: dataFolder,
                loadOrder: loadOrder,
                release: release,
                patchers: patchers.Select(p => (default(object?), p)),
                reporter: new WrapReporter(reporter ?? ThrowReporter.Instance),
                sourcePath: sourcePath,
                cancellation: cancellation);
        }

        public static async Task<bool> Run<TKey>(
            string workingDirectory,
            ModPath outputPath,
            string dataFolder,
            IEnumerable<LoadOrderListing> loadOrder,
            GameRelease release,
            IEnumerable<(TKey Key, IPatcherRun Run)> patchers,
            IRunReporter<TKey> reporter,
            ModPath? sourcePath = null,
            CancellationToken? cancellation = null)
        {
            try
            {
                cancellation ??= CancellationToken.None;
                if (sourcePath != null)
                {
                    if (!File.Exists(sourcePath))
                    {
                        reporter.ReportOverallProblem(new FileNotFoundException($"Source path did not exist: {sourcePath}"));
                        return false;
                    }
                }
                var dirInfo = new DirectoryInfo(workingDirectory);
                dirInfo.DeleteEntireFolder();
                dirInfo.Create();

                var patchersList = patchers.ToList();
                if (patchersList.Count == 0 || cancellation.Value.IsCancellationRequested) return false;

                bool problem = false;

                // Copy plugins text to working directory, trimming synthesis and anything after
                var loadOrderList = loadOrder.ToList();
                var synthIndex = loadOrderList.IndexOf(Constants.SynthesisModKey, (listing, key) => listing.ModKey == key);
                if (synthIndex != -1)
                {
                    loadOrderList.RemoveToCount(synthIndex);
                }
                string loadOrderPath = Path.Combine(workingDirectory, "Plugins.txt");
                var writeLoadOrder = Task.Run(() =>
                {
                    try
                    {
                        LoadOrder.Write(
                            loadOrderPath,
                            release,
                            loadOrderList);
                    }
                    catch (Exception ex)
                    {
                        reporter.ReportOverallProblem(ex);
                        problem = true;
                    }
                });

                // Start up prep for all patchers in background
                var patcherPreps = patchersList.Select(patcher => Task.Run(async () =>
                {
                    try
                    {
                        using var outputSub = patcher.Run.Output
                            .Subscribe(reporter.ReportOutput);
                        using var errorSub = patcher.Run.Error
                            .Subscribe(reporter.ReportError);
                        try
                        {
                            await patcher.Run.Prep(release, cancellation);
                        }
                        catch (TaskCanceledException)
                        {
                        }
                        catch (Exception ex)
                        {
                            reporter.ReportPrepProblem(patcher.Key, patcher.Run, ex);
                            return ex;
                        }
                    }
                    catch (TaskCanceledException)
                    {
                    }
                    catch (Exception ex)
                    {
                        reporter.ReportPrepProblem(patcher.Key, patcher.Run, ex);
                        return ex;
                    }
                    return default(Exception?);
                })).ToList();

                // Wait for load order, at least
                await writeLoadOrder;
                if (problem || cancellation.Value.IsCancellationRequested) return false;

                var prevPath = sourcePath;
                for (int i = 0; i < patchersList.Count; i++)
                {
                    // Finish waiting for prep, if it didn't finish
                    var prepException = await patcherPreps[i];
                    if (prepException != null) return false;

                    var patcher = patchersList[i];
                    var fileName = StringExt.RemoveDisallowedFilepathChars(patcher.Run.Name);
                    var nextPath = new ModPath(outputPath.ModKey, Path.Combine(workingDirectory, $"{i} - {fileName}"));
                    try
                    {
                        using var outputSub = patcher.Run.Output
                            .Subscribe(reporter.ReportOutput);
                        using var errorSub = patcher.Run.Error
                            .Subscribe(reporter.ReportError);

                        try
                        {
                            // Start run
                            reporter.ReportStartingRun(patcher.Key, patcher.Run);
                            await patcher.Run.Run(new RunSynthesisPatcher()
                            {
                                SourcePath = prevPath?.Path,
                                OutputPath = nextPath,
                                DataFolderPath = dataFolder,
                                GameRelease = release,
                                LoadOrderFilePath = loadOrderPath,
                            },
                            cancel: cancellation);
                        }
                        catch (TaskCanceledException)
                        {
                        }
                        catch (Exception ex)
                        {
                            reporter.ReportRunProblem(patcher.Key, patcher.Run, ex);
                            return false;
                        }
                    }
                    catch (TaskCanceledException)
                    {
                        return false;
                    }
                    catch (Exception ex)
                    {
                        reporter.ReportRunProblem(patcher.Key, patcher.Run, ex);
                        return false;
                    }
                    if (cancellation.Value.IsCancellationRequested) return false;
                    if (!File.Exists(nextPath))
                    {
                        reporter.ReportRunProblem(patcher.Key, patcher.Run, new ArgumentException($"Patcher {patcher.Run.Name} did not produce output file."));
                        return false;
                    }
                    reporter.ReportRunSuccessful(patcher.Key, patcher.Run, nextPath);
                    prevPath = nextPath;
                }
                File.Copy(prevPath!.Path, outputPath);
                return true;
            }
            catch (Exception ex)
            {
                reporter.ReportOverallProblem(ex);
                return false;
            }
        }
    }
}
