using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Mutagen.Bethesda.Environments.DI;
using Mutagen.Bethesda.Plugins.Order;
using Noggog;
using Synthesis.Bethesda.Commands;
using Synthesis.Bethesda.Execution.Patchers.Common;
using Synthesis.Bethesda.Execution.Placement;
using Synthesis.Bethesda.Execution.Utility;

namespace Synthesis.Bethesda.Execution.PatcherCommands
{
    public interface IOpenSettingsHost
    {
        Task<int> Open(
            FilePath path,
            IEnumerable<IModListingGetter> loadOrder,
            CancellationToken cancel);
    }

    [ExcludeFromCodeCoverage]
    public class OpenSettingsHost : IOpenSettingsHost
    {
        private readonly IGameReleaseContext _gameReleaseContext;
        private readonly IPatcherNameProvider _nameProvider;
        private readonly ITemporaryLoadOrderProvider _loadOrderProvider;
        private readonly IProcessRunner _processRunner;
        private readonly IDataDirectoryProvider _dataDirectoryProvider;
        private readonly IRunProcessStartInfoProvider _runProcessStartInfoProvider;
        private readonly IWindowPlacement _windowPlacement;

        public OpenSettingsHost(
            IGameReleaseContext gameReleaseContext,
            IPatcherNameProvider nameProvider,
            ITemporaryLoadOrderProvider loadOrderProvider,
            IProcessRunner processRunner,
            IDataDirectoryProvider dataDirectoryProvider,
            IRunProcessStartInfoProvider runProcessStartInfoProvider,
            IWindowPlacement windowPlacement)
        {
            _gameReleaseContext = gameReleaseContext;
            _nameProvider = nameProvider;
            _loadOrderProvider = loadOrderProvider;
            _processRunner = processRunner;
            _dataDirectoryProvider = dataDirectoryProvider;
            _runProcessStartInfoProvider = runProcessStartInfoProvider;
            _windowPlacement = windowPlacement;
        }
        
        public async Task<int> Open(
            FilePath path,
            IEnumerable<IModListingGetter> loadOrder,
            CancellationToken cancel)
        {
            using var loadOrderFile = _loadOrderProvider.Get(loadOrder);

            return await _processRunner.Run(
                _runProcessStartInfoProvider.GetStart("SettingsHost/Synthesis.Bethesda.SettingsHost.exe", directExe: true, new HostSettings()
                {
                    PatcherName = _nameProvider.Name,
                    PatcherPath = path,
                    Left = (int)_windowPlacement.Left,
                    Top = (int)_windowPlacement.Top,
                    Height = (int)_windowPlacement.Height,
                    Width = (int)_windowPlacement.Width,
                    LoadOrderFilePath = loadOrderFile.File.Path,
                    DataFolderPath = _dataDirectoryProvider.Path,
                    GameRelease = _gameReleaseContext.Release,
                }),
                cancel: cancel).ConfigureAwait(false);
        }
    }
}