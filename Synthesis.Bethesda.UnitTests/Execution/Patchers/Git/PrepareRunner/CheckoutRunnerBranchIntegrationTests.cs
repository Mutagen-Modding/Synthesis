using NSubstitute;
using Noggog.GitRepository;
using Synthesis.Bethesda.Execution.Patchers.Git.PrepareRunner;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.Patchers.Git.PrepareRunner;

public class CheckoutRunnerBranchIntegrationTests
{
    [Theory, SynthAutoData]
    public void TryCreatesRunnerBranch(
        IGitRepository repo,
        CheckoutRunnerBranch sut)
    {
        sut.Checkout(repo);
        repo.Received(1).TryCreateBranch(CheckoutRunnerBranch.RunnerBranch);
    }

    [Theory, SynthAutoData]
    public void ResetsHard(
        IGitRepository repo,
        CheckoutRunnerBranch sut)
    {
        sut.Checkout(repo);
        repo.Received(1).ResetHard();
    }

    [Theory, SynthAutoData]
    public void ChecksOutRunnerBranch(
        IGitRepository repo,
        IBranch branch,
        CheckoutRunnerBranch sut)
    {
        repo.TryCreateBranch(default!).ReturnsForAnyArgs(branch);
        sut.Checkout(repo);
        repo.Received(1).Checkout(branch);
    }

    [Theory, SynthAutoData]
    public void PipelineOrder(
        IGitRepository repo,
        CheckoutRunnerBranch sut)
    {
        sut.Checkout(repo);
        Received.InOrder(() =>
        {
            repo.TryCreateBranch(Arg.Any<string>());
            repo.ResetHard();
            repo.Checkout(Arg.Any<IBranch>());
        });
    }
}