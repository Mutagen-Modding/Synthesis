using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins.Order;
using Noggog;
using Noggog.Utility;
using Serilog;

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
            CancellationToken cancel);

        Task<ErrorResponse> Check(
            string path,
            bool directExe,
            GameRelease release,
            string dataFolder,
            string loadOrderPath,
            CancellationToken cancel);
    }

    public class CheckRunnability : ICheckRunnability
    {
        private readonly ILogger _Logger;
        private readonly IProcessFactory _processFactory;
        private readonly IProvideTemporaryLoadOrder _loadOrder;
        private readonly IProvideDotNetRunProcessInfo _runProcessInfo;

        public CheckRunnability(
            ILogger logger,
            IProcessFactory processFactory,
            IProvideTemporaryLoadOrder loadOrder,
            IProvideDotNetRunProcessInfo runProcessInfo)
        {
            _Logger = logger;
            _processFactory = processFactory;
            _loadOrder = loadOrder;
            _runProcessInfo = runProcessInfo;
        }
        
        public async Task<ErrorResponse> Check(
            string path,
            bool directExe,
            GameRelease release,
            string dataFolder,
            IEnumerable<IModListingGetter> loadOrder,
            CancellationToken cancel)
        {
            using var loadOrderFile = _loadOrder.Get(release, loadOrder);

            return await Check(
                path,
                directExe: directExe,
                release: release,
                dataFolder: dataFolder,
                loadOrderPath: loadOrderFile.File.Path,
                cancel: cancel);
        }

        public async Task<ErrorResponse> Check(
            string path,
            bool directExe,
            GameRelease release,
            string dataFolder,
            string loadOrderPath,
            CancellationToken cancel)
        {
            var checkState = new Synthesis.Bethesda.CheckRunnability()
            {
                DataFolderPath = dataFolder,
                GameRelease = release,
                LoadOrderFilePath = loadOrderPath
            };

            using var proc = _processFactory.Create(
                _runProcessInfo.GetStart(path, directExe, checkState),
                cancel: cancel);
            _Logger.Information("({WorkingDirectory}): {FileName} {Args}",
                proc.StartInfo.WorkingDirectory,
                proc.StartInfo.FileName,
                proc.StartInfo.Arguments);
            
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