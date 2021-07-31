using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mutagen.Bethesda.Environments.DI;
using Mutagen.Bethesda.Plugins.Order;
using Noggog.Utility;
using Synthesis.Bethesda.Execution.Placement;
using Synthesis.Bethesda.Execution.Utility;

namespace Synthesis.Bethesda.Execution.CLI
{
    public interface IExecuteOpenForSettings
    {
        Task<int> Open(
            string path,
            bool directExe,
            IEnumerable<IModListingGetter> loadOrder,
            CancellationToken cancel);
    }

    public class ExecuteOpenForSettings : IExecuteOpenForSettings
    {
        public IGameReleaseContext GameReleaseContext { get; }
        public IDataDirectoryProvider DataDirectoryProvider { get; }
        public IProcessRunner ProcessRunner { get; }
        public ITemporaryLoadOrderProvider LoadOrderProvider { get; }
        public IRunProcessStartInfoProvider RunProcessStartInfoProvider { get; }
        public IWindowPlacement WindowPlacement { get; }

        public ExecuteOpenForSettings(
            IGameReleaseContext gameReleaseContext,
            IDataDirectoryProvider dataDirectoryProvider,
            ITemporaryLoadOrderProvider loadOrderProvider,
            IProcessRunner processRunner,
            IRunProcessStartInfoProvider runProcessStartInfoProvider,
            IWindowPlacement windowPlacement)
        {
            GameReleaseContext = gameReleaseContext;
            DataDirectoryProvider = dataDirectoryProvider;
            ProcessRunner = processRunner;
            LoadOrderProvider = loadOrderProvider;
            RunProcessStartInfoProvider = runProcessStartInfoProvider;
            WindowPlacement = windowPlacement;
        }
        
        public async Task<int> Open(
            string path,
            bool directExe,
            IEnumerable<IModListingGetter> loadOrder,
            CancellationToken cancel)
        {
            using var loadOrderFile = LoadOrderProvider.Get(loadOrder);

            return await ProcessRunner.Run(
                RunProcessStartInfoProvider.GetStart(path, directExe, new Synthesis.Bethesda.OpenForSettings()
                {
                    Left = (int)WindowPlacement.Left,
                    Top = (int)WindowPlacement.Top,
                    Height = (int)WindowPlacement.Height,
                    Width = (int)WindowPlacement.Width,
                    LoadOrderFilePath = loadOrderFile.File.Path,
                    DataFolderPath = DataDirectoryProvider.Path,
                    GameRelease = GameReleaseContext.Release,
                }),
                cancel: cancel);
        }
    }
}