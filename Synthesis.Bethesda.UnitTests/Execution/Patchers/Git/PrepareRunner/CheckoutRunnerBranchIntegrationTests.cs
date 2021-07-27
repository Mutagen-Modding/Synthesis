using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using LibGit2Sharp;
using Synthesis.Bethesda.Execution.Patchers.Git.PrepareRunner;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.Patchers.Git.PrepareRunner
{
    public class CheckoutRunnerBranchIntegrationTests : RepoTestUtility
    {
        [Theory, SynthAutoData]
        public async Task CreatesRunnerBranch(
            CheckoutRunnerBranch sut)
        {
            using var tmp = GetRepository(
                nameof(CheckoutRunnerBranchIntegrationTests),
                out var remote, out var local);
            using var repo = new Repository(local);
            sut.Checkout(new Bethesda.Execution.GitRepository.GitRepository(repo));
            repo.Branches.Select(x => x.FriendlyName).Should().Contain(CheckoutRunnerBranch.RunnerBranch);
        }
    }
}