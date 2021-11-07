using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mutagen.Bethesda.Environments.DI;
using Noggog;
using Synthesis.Bethesda.Commands;
using Synthesis.Bethesda.Execution.Utility;

namespace Synthesis.Bethesda.Execution.PatcherCommands
{
    public interface IExecuteRunnabilityCheck
    {
        Task<ErrorResponse> Check(
            string path,
            bool directExe,
            string loadOrderPath,
            CancellationToken cancel);
    }

    public class ExecuteRunnabilityCheck : IExecuteRunnabilityCheck
    {
        public const int MaxLines = 100;
        
        public IGameReleaseContext GameReleaseContext { get; }
        public IProcessRunner ProcessRunner { get; }
        public IRunProcessStartInfoProvider RunProcessStartInfoProvider { get; }
        public IDataDirectoryProvider DataDirectoryProvider { get; }

        public ExecuteRunnabilityCheck(
            IGameReleaseContext gameReleaseContext,
            IProcessRunner processRunner,
            IDataDirectoryProvider dataDirectoryProvider,
            IRunProcessStartInfoProvider runProcessStartInfoProvider)
        {
            DataDirectoryProvider = dataDirectoryProvider;
            GameReleaseContext = gameReleaseContext;
            ProcessRunner = processRunner;
            RunProcessStartInfoProvider = runProcessStartInfoProvider;
        }

        public async Task<ErrorResponse> Check(
            string path,
            bool directExe,
            string loadOrderPath,
            CancellationToken cancel)
        {
            var results = new List<string>();
            void AddResult(string s)
            {
                if (results.Count < 100)
                {
                    results.Add(s);
                }
            }

            var checkState = new CheckRunnability()
            {
                DataFolderPath = DataDirectoryProvider.Path,
                GameRelease = GameReleaseContext.Release,
                LoadOrderFilePath = loadOrderPath
            };

            var result = await ProcessRunner.RunWithCallback(
                RunProcessStartInfoProvider.GetStart(path, directExe, checkState),
                AddResult,
                cancel: cancel).ConfigureAwait(false);

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