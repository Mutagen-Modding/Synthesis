using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mutagen.Bethesda.Plugins;
using NSubstitute;
using Synthesis.Bethesda.Execution.Groups;
using Synthesis.Bethesda.Execution.Running.Runner;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.Running.Runner;

public class OverallRunPreparerTests
{
    [Theory, SynthAutoData]
    public async Task PassesOutputToRunLoadOrderPreparer(
        IGroupRun groupRun,
        IReadOnlySet<ModKey> blacklist,
        GroupRunPreparer sut)
    {
        await sut.Prepare(groupRun, blacklist);
        sut.GroupRunLoadOrderPreparer.Received(1).Write(groupRun, blacklist);
    }
        
    [Theory, SynthAutoData]
    public async Task PassesPersistenceToPersister(
        IGroupRun groupRun,
        PersistenceMode persistenceMode,
        IReadOnlySet<ModKey> blacklist,
        string? persistencePath,
        GroupRunPreparer sut)
    {
        await sut.Prepare(groupRun, blacklist, persistenceMode, persistencePath);
        sut.PersistencePreparer.Received(1).Prepare(persistenceMode, persistencePath);
    }
        
    [Theory, SynthAutoData]
    public async Task ThrowingLoadOrderPreparerStillRunsPersistence(
        IGroupRun groupRun,
        PersistenceMode persistenceMode,
        IReadOnlySet<ModKey> blacklist,
        string? persistencePath,
        GroupRunPreparer sut)
    {
        sut.GroupRunLoadOrderPreparer.When(x => x.Write(groupRun, blacklist))
            .Do(_ => throw new NotImplementedException());
        await Assert.ThrowsAsync<NotImplementedException>(async () =>
        {
            await sut.Prepare(groupRun, blacklist, persistenceMode, persistencePath);
        });
        sut.PersistencePreparer.Received(1).Prepare(persistenceMode, persistencePath);
    }
        
    [Theory, SynthAutoData]
    public async Task ThrowingPersistencePrepareStillRunsLoadOrderPrepare(
        IGroupRun groupRun,
        PersistenceMode persistenceMode,
        IReadOnlySet<ModKey> blacklist,
        string? persistencePath,
        GroupRunPreparer sut)
    {
        sut.PersistencePreparer.When(x => x.Prepare(Arg.Any<PersistenceMode>(), Arg.Any<string?>()))
            .Do(_ => throw new NotImplementedException());
        await Assert.ThrowsAsync<NotImplementedException>(async () =>
        {
            await sut.Prepare(groupRun, blacklist, persistenceMode, persistencePath);
        });
        sut.GroupRunLoadOrderPreparer.Received(1).Write(groupRun, blacklist);
    }
}