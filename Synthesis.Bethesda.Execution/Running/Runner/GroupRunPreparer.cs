using Mutagen.Bethesda.Plugins;
using Noggog;
using Synthesis.Bethesda.Execution.Groups;
using Synthesis.Bethesda.Execution.Settings;

namespace Synthesis.Bethesda.Execution.Running.Runner;

public interface IGroupRunPreparer
{
    Task Prepare(
        IGroupRun groupRun,
        IReadOnlySet<ModKey> blackListedMods,
        RunParameters runParameters);
}

public class GroupRunPreparer : IGroupRunPreparer
{
    public IGroupRunLoadOrderPreparer GroupRunLoadOrderPreparer { get; }
    public IRunPersistencePreparer PersistencePreparer { get; }

    public GroupRunPreparer(
        IGroupRunLoadOrderPreparer groupRunLoadOrderPreparer,
        IRunPersistencePreparer persistencePreparer)
    {
        GroupRunLoadOrderPreparer = groupRunLoadOrderPreparer;
        PersistencePreparer = persistencePreparer;
    }
        
    public async Task Prepare(
        IGroupRun groupRun,
        IReadOnlySet<ModKey> blackListedMods,
        RunParameters runParameters)
    {
        await Task.WhenAll(
            Task.Run(() =>
            {
                GroupRunLoadOrderPreparer.Write(groupRun, blackListedMods);
            }), 
            Task.Run(() =>
            {
                PersistencePreparer.Prepare(runParameters.PersistenceMode, runParameters.PersistencePath);
            })).ConfigureAwait(false);
    }
}