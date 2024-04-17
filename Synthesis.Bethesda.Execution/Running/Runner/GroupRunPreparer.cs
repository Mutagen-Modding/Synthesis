using Mutagen.Bethesda.Plugins;
using Noggog;
using Synthesis.Bethesda.Execution.Groups;
using Synthesis.Bethesda.Execution.Settings;

namespace Synthesis.Bethesda.Execution.Running.Runner;

public interface IGroupRunPreparer
{
    Task<FilePath> Prepare(
        IGroupRun groupRun,
        IReadOnlySet<ModKey> blackListedMods,
        RunParameters runParameters);
}

public class GroupRunPreparer : IGroupRunPreparer
{
    private readonly ICreateEmptyPatch _createEmptyPatch;
    public IGroupRunLoadOrderPreparer GroupRunLoadOrderPreparer { get; }
    public IRunPersistencePreparer PersistencePreparer { get; }

    public GroupRunPreparer(
        IGroupRunLoadOrderPreparer groupRunLoadOrderPreparer,
        IRunPersistencePreparer persistencePreparer,
        ICreateEmptyPatch createEmptyPatch)
    {
        _createEmptyPatch = createEmptyPatch;
        GroupRunLoadOrderPreparer = groupRunLoadOrderPreparer;
        PersistencePreparer = persistencePreparer;
    }
        
    public async Task<FilePath> Prepare(
        IGroupRun groupRun,
        IReadOnlySet<ModKey> blackListedMods,
        RunParameters runParameters)
    {
        var seedPatch = Task.Run(() =>
        {
            return _createEmptyPatch.Create(groupRun.ModKey, runParameters);
        });
        await Task.WhenAll(
            Task.Run(() =>
            {
                GroupRunLoadOrderPreparer.Write(groupRun, blackListedMods);
            }), 
            Task.Run(() =>
            {
                PersistencePreparer.Prepare(runParameters.PersistenceMode, runParameters.PersistencePath);
            }),
            seedPatch).ConfigureAwait(false);

        return await seedPatch;
    }
}