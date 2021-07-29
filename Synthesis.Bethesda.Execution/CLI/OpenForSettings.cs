using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Environments.DI;
using Mutagen.Bethesda.Plugins.Order;
using Noggog.Utility;
using Synthesis.Bethesda.Execution.Placement;

namespace Synthesis.Bethesda.Execution.CLI
{
    public interface IOpenForSettings
    {
        Task<int> Open(
            string path,
            bool directExe,
            string dataFolderPath,
            IEnumerable<IModListingGetter> loadOrder,
            CancellationToken cancel);
    }

    public class OpenForSettings : IOpenForSettings
    {
        private readonly IGameReleaseContext _gameReleaseContext;
        private readonly IProvideTemporaryLoadOrder _LoadOrder;
        private readonly IProcessFactory _ProcessFactory;
        private readonly IProvideDotNetRunProcessInfo _runProcessInfo;
        private readonly IWindowPlacement _WindowPlacement;

        public OpenForSettings(
            IGameReleaseContext gameReleaseContext,
            IProvideTemporaryLoadOrder loadOrder,
            IProcessFactory processFactory,
            IProvideDotNetRunProcessInfo runProcessInfo,
            IWindowPlacement windowPlacement)
        {
            _gameReleaseContext = gameReleaseContext;
            _LoadOrder = loadOrder;
            _ProcessFactory = processFactory;
            _runProcessInfo = runProcessInfo;
            _WindowPlacement = windowPlacement;
        }
        
        public async Task<int> Open(
            string path,
            bool directExe,
            string dataFolderPath,
            IEnumerable<IModListingGetter> loadOrder,
            CancellationToken cancel)
        {
            using var loadOrderFile = _LoadOrder.Get(loadOrder);

            using var proc = _ProcessFactory.Create(
                _runProcessInfo.GetStart(path, directExe, new Synthesis.Bethesda.OpenForSettings()
                {
                    Left = (int)_WindowPlacement.Left,
                    Top = (int)_WindowPlacement.Top,
                    Height = (int)_WindowPlacement.Height,
                    Width = (int)_WindowPlacement.Width,
                    LoadOrderFilePath = loadOrderFile.File.Path,
                    DataFolderPath = dataFolderPath,
                    GameRelease = _gameReleaseContext.Release,
                }),
                cancel: cancel,
                hookOntoOutput: false);

            return await proc.Run();
        }
    }
}