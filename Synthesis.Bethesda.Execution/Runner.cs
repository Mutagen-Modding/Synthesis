using Synthesis.Bethesda.Execution.Patchers;
using Noggog;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Environments.DI;
using Synthesis.Bethesda.Execution.Reporters;
using Synthesis.Bethesda.Execution.Settings;
using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Allocators;
using Mutagen.Bethesda.Plugins.Implicit.DI;
using Mutagen.Bethesda.Plugins.Order.DI;
using Path = System.IO.Path;
using FileNotFoundException = System.IO.FileNotFoundException;

namespace Synthesis.Bethesda.Execution
{
    public class Runner
    {
        public static async Task<bool> Run(
            string workingDirectory,
            ModPath outputPath,
            string dataFolder,
            IEnumerable<IModListingGetter> loadOrder,
            GameRelease release,
            IEnumerable<IPatcherRun> patchers,
            CancellationToken cancel,
            ModPath? sourcePath = null,
            IRunReporter? reporter = null,
            PersistenceMode persistenceMode = PersistenceMode.None,
            string? persistencePath = null,
            IFileSystem? fileSystem = null)
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
                cancellation: cancel,
                persistenceMode: persistenceMode,
                persistencePath: persistencePath,
                fileSystem: fileSystem);
        }

        public static async Task<bool> Run<TKey>(
            string workingDirectory,
            ModPath outputPath,
            string dataFolder,
            IEnumerable<IModListingGetter> loadOrder,
            GameRelease release,
            IEnumerable<(TKey Key, IPatcherRun Run)> patchers,
            IRunReporter<TKey> reporter,
            CancellationToken cancellation,
            ModPath? sourcePath = null,
            PersistenceMode persistenceMode = PersistenceMode.None,
            string? persistencePath = null,
            IFileSystem? fileSystem = null)
        {
            try
            {
                fileSystem = fileSystem.GetOrDefault();
                if (sourcePath != null)
                {
                    if (!fileSystem.File.Exists(sourcePath))
                    {
                        reporter.ReportOverallProblem(new FileNotFoundException($"Source path did not exist: {sourcePath}"));
                        return false;
                    }
                }

                fileSystem.Directory.DeleteEntireFolder(workingDirectory);
                fileSystem.Directory.CreateDirectory(workingDirectory);

                var patchersList = patchers.ToList();
                if (patchersList.Count == 0 || cancellation.IsCancellationRequested) return false;

                bool problem = false;

                // Copy plugins text to working directory, trimming synthesis and anything after
                var loadOrderList = loadOrder.ToList();
                var synthIndex = loadOrderList.IndexOf(outputPath.ModKey, (listing, key) => listing.ModKey == key);
                if (synthIndex != -1)
                {
                    loadOrderList.RemoveToCount(synthIndex);
                }
                reporter.Write(default(TKey)!, default, "Load Order:");
                loadOrderList.WithIndex().ForEach(i => reporter.Write(default(TKey)!, default, $" [{i.Index,3}] {i.Item}"));
                string loadOrderPath = Path.Combine(workingDirectory, "Plugins.txt");

                var releaseContext = new GameReleaseInjection(release);
                var loadOrderWriter = new LoadOrderWriter(
                    fileSystem,
                    new HasEnabledMarkersProvider(releaseContext),
                    new ImplicitListingModKeyProvider(releaseContext));
                var writeLoadOrder = Task.Run(() =>
                {
                    try
                    {
                        loadOrderWriter.Write(
                            loadOrderPath,
                            loadOrderList,
                            removeImplicitMods: true);
                    }
                    catch (Exception ex)
                    {
                        reporter.ReportOverallProblem(ex);
                        problem = true;
                    }
                });

                // Prep up persistence systems
                var persistenceSetup = Task.Run(() =>
                {
                    try
                    {
                        switch (persistenceMode)
                        {
                            case PersistenceMode.None:
                                persistencePath = null;
                                break;
                            case PersistenceMode.Text:
                                TextFileSharedFormKeyAllocator.Initialize(persistencePath ?? throw new ArgumentNullException("Persistence mode specified, but no path provided"), fileSystem);
                                break;
                            default:
                                throw new NotImplementedException();
                        }
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
                            .Subscribe(i => reporter.Write(patcher.Key, patcher.Run, i));
                        using var errorSub = patcher.Run.Error
                            .Subscribe(i => reporter.WriteError(patcher.Key, patcher.Run, i));
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

                // Wait for load order and persistence, at least
                await Task.WhenAll(writeLoadOrder, persistenceSetup);
                if (problem || cancellation.IsCancellationRequested) return false;

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
                            .Subscribe(i => reporter.Write(patcher.Key, patcher.Run, i));
                        using var errorSub = patcher.Run.Error
                            .Subscribe(i => reporter.WriteError(patcher.Key, patcher.Run, i));

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
                                PersistencePath = persistencePath,
                                PatcherName = fileName
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
                    if (cancellation.IsCancellationRequested) return false;
                    if (!fileSystem.File.Exists(nextPath))
                    {
                        reporter.ReportRunProblem(patcher.Key, patcher.Run, new ArgumentException($"Patcher {patcher.Run.Name} did not produce output file."));
                        return false;
                    }
                    reporter.ReportRunSuccessful(patcher.Key, patcher.Run, nextPath);
                    prevPath = nextPath;
                }
                if (fileSystem.File.Exists(outputPath))
                {
                    fileSystem.File.Delete(outputPath);
                }
                fileSystem.File.Copy(prevPath!.Path, outputPath);
                reporter.Write(default!, default, $"Exported patch to: {outputPath}");
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
