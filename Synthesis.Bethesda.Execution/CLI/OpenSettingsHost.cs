using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins.Order;
using Noggog.Utility;

namespace Synthesis.Bethesda.Execution.CLI
{
    public interface IOpenSettingsHost
    {
        Task<int> Open(
            string patcherName,
            string path,
            GameRelease release,
            string dataFolderPath,
            IEnumerable<IModListingGetter> loadOrder,
            Rectangle rect,
            CancellationToken cancel);
    }

    public class OpenSettingsHost : IOpenSettingsHost
    {
        private readonly IProvideTemporaryLoadOrder _LoadOrder;
        private readonly IProvideDotNetProcessInfo _ProcessInfo;

        public OpenSettingsHost(
            IProvideTemporaryLoadOrder loadOrder,
            IProvideDotNetProcessInfo processInfo)
        {
            _LoadOrder = loadOrder;
            _ProcessInfo = processInfo;
        }
        
        public async Task<int> Open(
            string patcherName,
            string path,
            GameRelease release,
            string dataFolderPath,
            IEnumerable<IModListingGetter> loadOrder,
            Rectangle rect,
            CancellationToken cancel)
        {
            using var loadOrderFile = _LoadOrder.Get(release, loadOrder);

            using var proc = ProcessWrapper.Create(
                _ProcessInfo.GetStart("SettingsHost/Synthesis.Bethesda.SettingsHost.exe", directExe: true, new Synthesis.Bethesda.HostSettings()
                {
                    PatcherName = patcherName,
                    PatcherPath = path,
                    Left = rect.Left,
                    Top = rect.Top,
                    Height = rect.Height,
                    Width = rect.Width,
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