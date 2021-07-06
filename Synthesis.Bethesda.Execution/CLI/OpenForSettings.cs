using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using Mutagen.Bethesda;
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
            GameRelease release,
            string dataFolderPath,
            IEnumerable<IModListingGetter> loadOrder,
            CancellationToken cancel);
    }

    public class OpenForSettings : IOpenForSettings
    {
        private readonly IProvideTemporaryLoadOrder _LoadOrder;
        private readonly IProcessFactory _ProcessFactory;
        private readonly IProvideDotNetProcessInfo _ProcessInfo;
        private readonly IWindowPlacement _WindowPlacement;

        public OpenForSettings(
            IProvideTemporaryLoadOrder loadOrder,
            IProcessFactory processFactory,
            IProvideDotNetProcessInfo processInfo,
            IWindowPlacement windowPlacement)
        {
            _LoadOrder = loadOrder;
            _ProcessFactory = processFactory;
            _ProcessInfo = processInfo;
            _WindowPlacement = windowPlacement;
        }
        
        public async Task<int> Open(
            string path,
            bool directExe,
            GameRelease release,
            string dataFolderPath,
            IEnumerable<IModListingGetter> loadOrder,
            CancellationToken cancel)
        {
            using var loadOrderFile = _LoadOrder.Get(release, loadOrder);

            using var proc = _ProcessFactory.Create(
                _ProcessInfo.GetStart(path, directExe, new Synthesis.Bethesda.OpenForSettings()
                {
                    Left = (int)_WindowPlacement.Left,
                    Top = (int)_WindowPlacement.Top,
                    Height = (int)_WindowPlacement.Height,
                    Width = (int)_WindowPlacement.Width,
                    LoadOrderFilePath = loadOrderFile.File.Path,
                    DataFolderPath = dataFolderPath,
                    GameRelease = release,
                }),
                cancel: cancel,
                hookOntoOutput: false);

            return await proc.Run();
        }
    }
}