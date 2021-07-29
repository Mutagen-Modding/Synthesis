using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Environments.DI;
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
            string dataFolder,
            string loadOrderPath,
            CancellationToken cancel);
    }

    public class CheckRunnability : ICheckRunnability
    {
        private readonly ILogger _Logger;
        private readonly IGameReleaseContext _gameReleaseContext;
        private readonly IProcessFactory _processFactory;
        private readonly IProvideDotNetRunProcessInfo _runProcessInfo;

        public CheckRunnability(
            ILogger logger,
            IGameReleaseContext gameReleaseContext,
            IProcessFactory processFactory,
            IProvideDotNetRunProcessInfo runProcessInfo)
        {
            _Logger = logger;
            _gameReleaseContext = gameReleaseContext;
            _processFactory = processFactory;
            _runProcessInfo = runProcessInfo;
        }

        public async Task<ErrorResponse> Check(
            string path,
            bool directExe,
            string dataFolder,
            string loadOrderPath,
            CancellationToken cancel)
        {
            var checkState = new Synthesis.Bethesda.CheckRunnability()
            {
                DataFolderPath = dataFolder,
                GameRelease = _gameReleaseContext.Release,
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