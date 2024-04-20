using Noggog;
using Synthesis.Bethesda.Execution.Groups;

namespace Synthesis.Bethesda.Execution.Running.Runner;

public interface IRunAllGroups
{
    Task Run(
        IGroupRun[] groups,
        CancellationToken cancellation,
        DirectoryPath outputDir,
        RunParameters runParameters);
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
        RunParameters runParameters)
    {
        for (int i = 0; i < groups.Length; i++)
        {
            var group = groups[i];

            var succeeded = await RunAGroup.Run(
                group,
                cancellation,
                outputDir,
                runParameters).ConfigureAwait(false);

            if (!succeeded) break;
        }
    }
}