using Shouldly;
using NSubstitute;
using Noggog.GitRepository;
using Synthesis.Bethesda.UnitTests.AutoData;

namespace Synthesis.Bethesda.UnitTests.Execution.GitRepository;

public class ResetToLatestMainTests
{
    [Theory, SynthAutoData]
    public void MainBranchNullFails(
        IGitRepository repo,
        ResetToLatestMain sut)
    {
        repo.MainBranch.Returns(default(IBranch?));
        sut.TryReset(repo)
            .Succeeded.ShouldBeFalse();
    }
        
    [Theory, SynthAutoData]
    public void ReturnsMainBranch(
        IBranch branch,
        IGitRepository repo,
        ResetToLatestMain sut)
    {
        repo.MainBranch.Returns(branch);
        var resp = sut.TryReset(repo);
        resp.Succeeded.ShouldBeTrue();
        resp.Value.ShouldBe(branch);
    }
        
    [Theory, SynthAutoData]
    public void ChecksOutMainBranch(
        IGitRepository repo,
        ResetToLatestMain sut)
    {
        var branch = Substitute.For<IBranch>();
        repo.MainBranch.Returns(branch);
        sut.TryReset(repo);
        repo.Received(1).Checkout(branch);
    }
        
    [Theory, SynthAutoData]
    public void GeneralPipelineOrder(
        IGitRepository repo,
        ResetToLatestMain sut)
    {
        var branch = Substitute.For<IBranch>();
        repo.MainBranch.Returns(branch);
        sut.TryReset(repo);
        Received.InOrder(() =>
        {
            repo.ResetHard();
            repo.Checkout(Arg.Any<IBranch>());
            repo.Pull();
        });
    }
}