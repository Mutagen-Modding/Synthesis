using System.Collections.Generic;
using System.Threading.Tasks;
using Mutagen.Bethesda.Plugins;
using Synthesis.Bethesda.Execution.Groups;
using Synthesis.Bethesda.Execution.Settings;

namespace Synthesis.Bethesda.Execution.Running.Runner;

public interface IGroupRunPreparer
{
    Task Prepare(
        IGroupRun groupRun,
        IReadOnlySet<ModKey> blackListedMods,
        PersistenceMode persistenceMode = PersistenceMode.None,
        string? persistencePath = null);
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
        PersistenceMode persistenceMode = PersistenceMode.None,
        string? persistencePath = null)
    {
        await Task.WhenAll(
            Task.Run(() =>
            {
                GroupRunLoadOrderPreparer.Write(groupRun, blackListedMods);
            }), 
            Task.Run(() =>
            {
                PersistencePreparer.Prepare(persistenceMode, persistencePath);
            })).ConfigureAwait(false);
    }
}