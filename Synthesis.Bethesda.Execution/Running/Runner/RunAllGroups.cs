using System;
using System.Threading;
using System.Threading.Tasks;
using Mutagen.Bethesda.Plugins;
using Noggog;
using Synthesis.Bethesda.Execution.Groups;
using Synthesis.Bethesda.Execution.Settings;

namespace Synthesis.Bethesda.Execution.Running.Runner
{
    public interface IRunAllGroups
    {
        Task Run(
            IGroupRun[] groups,
            CancellationToken cancellation,
            DirectoryPath outputDir,
            RunParameters runParameters,
            FilePath? sourcePath = null);
    }

    public class RunAllGroups : IRunAllGroups
    {
        public IRunAGroup RunAGroup { get; }

        public RunAllGroups(IRunAGroup runAGroup)
        {
            RunAGroup = runAGroup;
        }

        public async Task Run(
            IGroupRun[] groups,
            CancellationToken cancellation,
            DirectoryPath outputDir,
            RunParameters runParameters,
            FilePath? sourcePath = null)
        {
            for (int i = 0; i < groups.Length; i++)
            {
                var group = groups[i];

                var succeeded = await RunAGroup.Run(
                    group,
                    cancellation,
                    outputDir,
                    runParameters,
                    sourcePath).ConfigureAwait(false);

                if (!succeeded) break;
            }
        }
    }
}