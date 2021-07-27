using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Noggog;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Patchers.Git.PrepareRunner;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.Patchers.Git.PrepareRunner
{
    public class PrepareRunnerRepositoryTests
    {
        [Theory, SynthAutoData]
        public async Task CancellationDoesNotThrow(
            string proj,
            DirectoryPath localRepoDir,
            GitPatcherVersioning patcherVersioning,
            NugetVersioningTarget nugetVersioning,
            CancellationToken cancelledToken,
            PrepareRunnerRepository sut)
        {
            (await sut.Checkout(proj, localRepoDir, patcherVersioning, nugetVersioning, cancelledToken))
                .RunnableState.Succeeded.Should().BeFalse();
        }
    }
}