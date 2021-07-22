using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins.Order;
using Noggog.Utility;
using Synthesis.Bethesda.Execution.Patchers;
using Synthesis.Bethesda.Execution.Patchers.TopLevel;
using Synthesis.Bethesda.Execution.Placement;

namespace Synthesis.Bethesda.Execution.CLI
{
    public interface IOpenSettingsHost
    {
        Task<int> Open(
            string path,
            GameRelease release,
            string dataFolderPath,
            IEnumerable<IModListingGetter> loadOrder,
            CancellationToken cancel);
    }

    public class OpenSettingsHost : IOpenSettingsHost
    {
        private readonly IPatcherNameProvider _nameProvider;
        private readonly IProvideTemporaryLoadOrder _LoadOrder;
        private readonly IProcessFactory _ProcessFactory;
        private readonly IProvideDotNetRunProcessInfo _runProcessInfo;
        private readonly IWindowPlacement _WindowPlacement;

        public OpenSettingsHost(
            IPatcherNameProvider nameProvider,
            IProvideTemporaryLoadOrder loadOrder,
            IProcessFactory processFactory,
            IProvideDotNetRunProcessInfo runProcessInfo,
            IWindowPlacement windowPlacement)
        {
            _nameProvider = nameProvider;
            _LoadOrder = loadOrder;
            _ProcessFactory = processFactory;
            _runProcessInfo = runProcessInfo;
            _WindowPlacement = windowPlacement;
        }
        
        public async Task<int> Open(
            string path,
            GameRelease release,
            string dataFolderPath,
            IEnumerable<IModListingGetter> loadOrder,
            CancellationToken cancel)
        {
            using var loadOrderFile = _LoadOrder.Get(release, loadOrder);

            using var proc = _ProcessFactory.Create(
                _runProcessInfo.GetStart("SettingsHost/Synthesis.Bethesda.SettingsHost.exe", directExe: true, new Synthesis.Bethesda.HostSettings()
                {
                    PatcherName = _nameProvider.Name,
                    PatcherPath = path,
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