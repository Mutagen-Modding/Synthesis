using Mutagen.Bethesda.Plugins;
using NSubstitute;
using Synthesis.Bethesda.Execution.Groups;
using Synthesis.Bethesda.Execution.Running.Runner;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.UnitTests.AutoData;

namespace Synthesis.Bethesda.UnitTests.Execution.Running.Runner;

public class OverallRunPreparerTests
{
    [Theory, SynthAutoData]
    public async Task PassesOutputToRunLoadOrderPreparer(
        IGroupRun groupRun,
        IReadOnlySet<ModKey> blacklist,
        RunParameters runParameters,
        GroupRunPreparer sut)
    {
        await sut.Prepare(groupRun, blacklist, runParameters);
        sut.GroupRunLoadOrderPreparer.Received(1).Write(groupRun, blacklist);
    }
        
    [Theory, SynthAutoData]
    public async Task PassesPersistenceToPersister(
        IGroupRun groupRun,
        IReadOnlySet<ModKey> blacklist,
        RunParameters runParameters,
        GroupRunPreparer sut)
    {
        await sut.Prepare(groupRun, blacklist, runParameters);
        sut.PersistencePreparer.Received(1)
            .Prepare(
                runParameters.PersistenceMode, 
                runParameters.PersistencePath);
    }
        
    [Theory, SynthAutoData]
    public async Task ThrowingLoadOrderPreparerStillRunsPersistence(
        IGroupRun groupRun,
        IReadOnlySet<ModKey> blacklist,
        RunParameters runParameters,
        GroupRunPreparer sut)
    {
        sut.GroupRunLoadOrderPreparer.When(x => x.Write(groupRun, blacklist))
            .Do(_ => throw new NotImplementedException());
        await Assert.ThrowsAsync<NotImplementedException>(async () =>
        {
            await sut.Prepare(groupRun, blacklist, runParameters);
        });
        sut.PersistencePreparer.Received(1)
            .Prepare(
                runParameters.PersistenceMode, 
                runParameters.PersistencePath);
    }
        
    [Theory, SynthAutoData]
    public async Task ThrowingPersistencePrepareStillRunsLoadOrderPrepare(
        IGroupRun groupRun,
        IReadOnlySet<ModKey> blacklist,
        RunParameters runParameters,
        GroupRunPreparer sut)
    {
        sut.PersistencePreparer.When(x => x.Prepare(Arg.Any<PersistenceMode>(), Arg.Any<string?>()))
            .Do(_ => throw new NotImplementedException());
        await Assert.ThrowsAsync<NotImplementedException>(async () =>
        {
            await sut.Prepare(groupRun, blacklist, runParameters);
        });
        sut.GroupRunLoadOrderPreparer.Received(1).Write(groupRun, blacklist);
    }
}