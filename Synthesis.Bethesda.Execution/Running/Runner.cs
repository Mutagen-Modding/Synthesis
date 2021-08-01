using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mutagen.Bethesda.Environments.DI;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Allocators;
using Mutagen.Bethesda.Plugins.Order.DI;
using Noggog;
using Synthesis.Bethesda.Execution.Patchers.Running;
using Synthesis.Bethesda.Execution.Pathing;
using Synthesis.Bethesda.Execution.Profile;
using Synthesis.Bethesda.Execution.Reporters;
using Synthesis.Bethesda.Execution.Settings;
using Path = System.IO.Path;
using FileNotFoundException = System.IO.FileNotFoundException;

namespace Synthesis.Bethesda.Execution.Running
{
    public interface IRunner
    {
        Task<bool> Run(
            ModPath outputPath,
            IEnumerable<(int Key, IPatcherRun Run)> patchers,
            CancellationToken cancel,
            ModPath? sourcePath = null,
            PersistenceMode persistenceMode = PersistenceMode.None,
            string? persistencePath = null);
    }

    public class Runner : IRunner
    {
        private readonly IFileSystem _FileSystem;
        private readonly IGameReleaseContext _ReleaseContext;
        private readonly IProfileDirectories _profileDirectories;
        private readonly IDataDirectoryProvider _DataDirectoryProvider;
        private readonly IRunReporter _reporter;
        private readonly ILoadOrderListingsProvider _LoadOrderListingsProvider;
        private readonly ILoadOrderWriter _LoadOrderWriter;

        public Runner(
            IFileSystem fileSystem,
            IGameReleaseContext releaseContext,
            IProfileDirectories profileDirectories,
            IDataDirectoryProvider dataDirectoryProvider,
            IRunReporter reporter,
            ILoadOrderListingsProvider loadOrderListingsProvider,
            ILoadOrderWriter loadOrderWriter)
        {
            _FileSystem = fileSystem;
            _ReleaseContext = releaseContext;
            _profileDirectories = profileDirectories;
            _DataDirectoryProvider = dataDirectoryProvider;
            _reporter = reporter;
            _LoadOrderListingsProvider = loadOrderListingsProvider;
            _LoadOrderWriter = loadOrderWriter;
        }
        
        public async Task<bool> Run(
            ModPath outputPath,
            IEnumerable<(int Key, IPatcherRun Run)> patchers,
            CancellationToken cancellation,
            ModPath? sourcePath = null,
            PersistenceMode persistenceMode = PersistenceMode.None,
            string? persistencePath = null)
        {
            try
            {
                if (sourcePath != null)
                {
                    if (!_FileSystem.File.Exists(sourcePath))
                    {
                        _reporter.ReportOverallProblem(new FileNotFoundException($"Source path did not exist: {sourcePath}"));
                        return false;
                    }
                }

                var workingDirectory = _profileDirectories.WorkingDirectory;
                _FileSystem.Directory.DeleteEntireFolder(workingDirectory);
                _FileSystem.Directory.CreateDirectory(workingDirectory);

                var patchersList = patchers.ToList();
                if (patchersList.Count == 0 || cancellation.IsCancellationRequested) return false;

                bool problem = false;

                // Copy plugins text to working directory, trimming synthesis and anything after
                var loadOrderList = _LoadOrderListingsProvider.Get().ToList();
                var synthIndex = loadOrderList.IndexOf(outputPath.ModKey, (listing, key) => listing.ModKey == key);
                if (synthIndex != -1)
                {
                    loadOrderList.RemoveToCount(synthIndex);
                }
                _reporter.Write(default(int), default, "Load Order:");
                loadOrderList.WithIndex().ForEach(i => _reporter.Write(default(int), default, $" [{i.Index,3}] {i.Item}"));
                string loadOrderPath = Path.Combine(workingDirectory, "Plugins.txt");

                var writeLoadOrder = Task.Run(() =>
                {
                    try
                    {
                        _LoadOrderWriter.Write(
                            loadOrderPath,
                            loadOrderList,
                            removeImplicitMods: true);
                    }
                    catch (Exception ex)
                    {
                        _reporter.ReportOverallProblem(ex);
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
                                TextFileSharedFormKeyAllocator.Initialize(persistencePath ?? throw new ArgumentNullException("Persistence mode specified, but no path provided"), _FileSystem);
                                break;
                            default:
                                throw new NotImplementedException();
                        }
                    }
                    catch (Exception ex)
                    {
                        _reporter.ReportOverallProblem(ex);
                        problem = true;
                    }
                });

                // Start up prep for all patchers in background
                var patcherPreps = patchersList.Select(patcher => Task.Run(async () =>
                {
                    try
                    {
                        try
                        {
                            await patcher.Run.Prep(cancellation);
                        }
                        catch (TaskCanceledException)
                        {
                        }
                        catch (Exception ex)
                        {
                            _reporter.ReportPrepProblem(patcher.Key, patcher.Run.Name, ex);
                            return ex;
                        }
                    }
                    catch (TaskCanceledException)
                    {
                    }
                    catch (Exception ex)
                    {
                        _reporter.ReportPrepProblem(patcher.Key, patcher.Run.Name, ex);
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
                        try
                        {
                            // Start run
                            _reporter.ReportStartingRun(patcher.Key, patcher.Run.Name);
                            await patcher.Run.Run(new RunSynthesisPatcher()
                            {
                                SourcePath = prevPath?.Path,
                                OutputPath = nextPath,
                                DataFolderPath = _DataDirectoryProvider.Path,
                                GameRelease = _ReleaseContext.Release,
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
                            _reporter.ReportRunProblem(patcher.Key, patcher.Run.Name, ex);
                            return false;
                        }
                    }
                    catch (TaskCanceledException)
                    {
                        return false;
                    }
                    catch (Exception ex)
                    {
                        _reporter.ReportRunProblem(patcher.Key, patcher.Run.Name, ex);
                        return false;
                    }
                    if (cancellation.IsCancellationRequested) return false;
                    if (!_FileSystem.File.Exists(nextPath))
                    {
                        _reporter.ReportRunProblem(patcher.Key, patcher.Run.Name, new ArgumentException($"Patcher {patcher.Run.Name} did not produce output file."));
                        return false;
                    }
                    _reporter.ReportRunSuccessful(patcher.Key, patcher.Run.Name, nextPath);
                    prevPath = nextPath;
                }
                if (_FileSystem.File.Exists(outputPath))
                {
                    _FileSystem.File.Delete(outputPath);
                }
                _FileSystem.File.Copy(prevPath!.Path, outputPath);
                _reporter.Write(default!, default, $"Exported patch to: {outputPath}");
                return true;
            }
            catch (Exception ex)
            {
                _reporter.ReportOverallProblem(ex);
                return false;
            }
        }
    }
}
