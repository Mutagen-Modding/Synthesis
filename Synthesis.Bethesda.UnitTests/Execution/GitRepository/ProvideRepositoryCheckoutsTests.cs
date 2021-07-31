using System;
using System.Threading.Tasks;
using AutoFixture;
using FluentAssertions;
using Noggog;
using Serilog;
using Synthesis.Bethesda.Execution.GitRepository;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.GitRepository
{
    public class ProvideRepositoryCheckoutsTests : RepoTestUtility
    {
        [Theory, SynthAutoData]
        public void TypicalGet(ProvideRepositoryCheckouts sut)
        {
            using var repoPath = GetRepository(
                nameof(ProvideRepositoryCheckoutsTests),
                out var remote, out var local,
                createPatcherFiles: false);
            using var checkout = sut.Get(local);
            new DirectoryPath(checkout.Repository.WorkingDirectory).Should().BeEquivalentTo(local);
        }

        [Theory, SynthAutoData]
        public void CleanShutdown(ProvideRepositoryCheckouts sut)
        {
            using var repoPath = GetRepository(
                nameof(ProvideRepositoryCheckoutsTests),
                out var remote, out var local,
                createPatcherFiles: false);
            sut.Get(local).Dispose();
            sut.Dispose();
            sut.IsShutdownRequested.Should().BeTrue();
            sut.IsShutdown.Should().BeTrue();
        }

        [Theory, SynthAutoData]
        public async Task BlockedShutdown(ProvideRepositoryCheckouts sut)
        {
            using var repoPath = GetRepository(
                nameof(ProvideRepositoryCheckoutsTests),
                out var remote, out var local,
                createPatcherFiles: false);
            var checkout = sut.Get(local);
            var waited = false;
            var t = Task.Run(async () =>
            {
                await Task.Delay(500);
                sut.IsShutdownRequested.Should().BeTrue();
                waited = true;
                checkout.Dispose();
            });
            sut.Dispose();
            waited.Should().BeTrue();
            await t;
            sut.IsShutdown.Should().BeTrue();
        }

        [Theory, SynthAutoData]
        public async Task RequestAfterShutdownThrows(
            DirectoryPath dir,
            ProvideRepositoryCheckouts sut)
        {
            sut.Dispose();
            Assert.Throws<InvalidOperationException>(() =>
            {
                sut.Get(dir);
            });
        }
    }
}