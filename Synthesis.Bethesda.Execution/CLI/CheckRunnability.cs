using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins.Order;
using Noggog;
using Noggog.Utility;

namespace Synthesis.Bethesda.Execution.CLI
{
    public interface ICheckRunnability
    {
        Task<ErrorResponse> Check(
            string path,
            bool directExe,
            GameRelease release,
            string dataFolder,
            IEnumerable<IModListingGetter> loadOrder,
            CancellationToken cancel,
            Action<string>? log);

        Task<ErrorResponse> Check(
            string path,
            bool directExe,
            GameRelease release,
            string dataFolder,
            string loadOrderPath,
            CancellationToken cancel,
            Action<string>? log);
    }

    public class CheckRunnability : ICheckRunnability
    {
        private readonly IProcessFactory _ProcessFactory;
        private readonly IProvideTemporaryLoadOrder _LoadOrder;
        private readonly IProvideDotNetRunProcessInfo _runProcessInfo;

        public CheckRunnability(
            IProcessFactory processFactory,
            IProvideTemporaryLoadOrder loadOrder,
            IProvideDotNetRunProcessInfo runProcessInfo)
        {
            _ProcessFactory = processFactory;
            _LoadOrder = loadOrder;
            _runProcessInfo = runProcessInfo;
        }
        
        public async Task<ErrorResponse> Check(
            string path,
            bool directExe,
            GameRelease release,
            string dataFolder,
            IEnumerable<IModListingGetter> loadOrder,
            CancellationToken cancel,
            Action<string>? log)
        {
            using var loadOrderFile = _LoadOrder.Get(release, loadOrder);

            return await Check(
                path,
                directExe: directExe,
                release: release,
                dataFolder: dataFolder,
                loadOrderPath: loadOrderFile.File.Path,
                cancel: cancel,
                log: log);
        }

        public async Task<ErrorResponse> Check(
            string path,
            bool directExe,
            GameRelease release,
            string dataFolder,
            string loadOrderPath,
            CancellationToken cancel,
            Action<string>? log)
        {
            var checkState = new Synthesis.Bethesda.CheckRunnability()
            {
                DataFolderPath = dataFolder,
                GameRelease = release,
                LoadOrderFilePath = loadOrderPath
            };

            using var proc = _ProcessFactory.Create(
                _runProcessInfo.GetStart(path, directExe, checkState),
                cancel: cancel);
            
            log?.Invoke($"({proc.StartInfo.WorkingDirectory}): {proc.StartInfo.FileName} {proc.StartInfo.Arguments}");

            var results = new List<string>();
            void AddResult(string s)
            {
                if (results.Count < 100)
                {
                    results.Add(s);
                }
            }
            using var ouputSub = proc.Output.Subscribe(AddResult);
            using var errSub = proc.Error.Subscribe(AddResult);

            var result = await proc.Run();

            if (result == (int)Codes.NotRunnable)
            {
                return ErrorResponse.Fail(string.Join(Environment.NewLine, results));
            }

            // Other error codes are likely the target app just not handling runnability checks, so return as runnable unless
            // explicitly told otherwise with the above error code
            return ErrorResponse.Success;
        }
    }
}