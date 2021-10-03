using System;
using System.Threading.Tasks;
using Mutagen.Bethesda.Plugins;
using NSubstitute;
using Synthesis.Bethesda.Execution.Running.Runner;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.Running.Runner
{
    public class OverallRunPreparerTests
    {
        [Theory, SynthAutoData]
        public async Task PassesOutputToRunLoadOrderPreparer(
            ModPath outputPath,
            GroupRunPreparer sut)
        {
            await sut.Prepare(outputPath);
            sut.RunLoadOrderPreparer.Received(1).Write(outputPath);
        }
        
        [Theory, SynthAutoData]
        public async Task PassesPersistenceToPersister(
            ModPath outputPath,
            PersistenceMode persistenceMode,
            string? persistencePath,
            GroupRunPreparer sut)
        {
            await sut.Prepare(outputPath, persistenceMode, persistencePath);
            sut.PersistencePreparer.Received(1).Prepare(persistenceMode, persistencePath);
        }
        
        [Theory, SynthAutoData]
        public async Task ThrowingLoadOrderPreparerStillRunsPersistence(
            ModPath outputPath,
            PersistenceMode persistenceMode,
            string? persistencePath,
            GroupRunPreparer sut)
        {
            sut.RunLoadOrderPreparer.When(x => x.Write(outputPath))
                .Do(_ => throw new NotImplementedException());
            await Assert.ThrowsAsync<NotImplementedException>(async () =>
            {
                await sut.Prepare(outputPath, persistenceMode, persistencePath);
            });
            sut.PersistencePreparer.Received(1).Prepare(persistenceMode, persistencePath);
        }
        
        [Theory, SynthAutoData]
        public async Task ThrowingPersistencePrepareStillRunsLoadOrderPrepare(
            ModPath outputPath,
            PersistenceMode persistenceMode,
            string? persistencePath,
            GroupRunPreparer sut)
        {
            sut.PersistencePreparer.When(x => x.Prepare(Arg.Any<PersistenceMode>(), Arg.Any<string?>()))
                .Do(_ => throw new NotImplementedException());
            await Assert.ThrowsAsync<NotImplementedException>(async () =>
            {
                await sut.Prepare(outputPath, persistenceMode, persistencePath);
            });
            sut.RunLoadOrderPreparer.Received(1).Write(outputPath);
        }
    }
}