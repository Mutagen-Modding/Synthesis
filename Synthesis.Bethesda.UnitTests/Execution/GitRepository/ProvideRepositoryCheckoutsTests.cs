using System.Threading.Tasks;
using AutoFixture;
using FluentAssertions;
using Noggog;
using Serilog;
using Synthesis.Bethesda.Execution.GitRespository;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.GitRepository
{
    public class ProvideRepositoryCheckoutsTests : RepoTestUtility, IClassFixture<Fixture>
    {
        private readonly Fixture _Fixture;

        public ProvideRepositoryCheckoutsTests(Fixture fixture)
        {
            _Fixture = fixture;
        }

        private ProvideRepositoryCheckouts Get()
        {
            return new(_Fixture.Inject.Create<ILogger>());
        }

        [Fact]
        public void TypicalGet()
        {
            using var repoPath = GetRepository(
                nameof(ProvideRepositoryCheckoutsTests),
                out var remote, out var local,
                createPatcherFiles: false);
            var provide = Get();
            using var checkout = provide.Get(local);
            new DirectoryPath(checkout.Repository.WorkingDirectory).Should().BeEquivalentTo(local);
        }

        [Fact]
        public void CleanShutdown()
        {
            using var repoPath = GetRepository(
                nameof(ProvideRepositoryCheckoutsTests),
                out var remote, out var local,
                createPatcherFiles: false);
            var provide = Get();
            provide.Get(local).Dispose();
            provide.Dispose();
            provide.IsShutdownRequested.Should().BeTrue();
            provide.IsShutdown.Should().BeTrue();
        }

        [Fact]
        public async Task BlockedShutdown()
        {
            using var repoPath = GetRepository(
                nameof(ProvideRepositoryCheckoutsTests),
                out var remote, out var local,
                createPatcherFiles: false);
            var provide = Get();
            var checkout = provide.Get(local);
            var waited = false;
            var t = Task.Run(async () =>
            {
                await Task.Delay(500);
                provide.IsShutdownRequested.Should().BeTrue();
                waited = true;
                checkout.Dispose();
            });
            provide.Dispose();
            waited.Should().BeTrue();
            await t;
            provide.IsShutdown.Should().BeTrue();
        }
    }
}