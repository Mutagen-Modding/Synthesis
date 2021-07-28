using FluentAssertions;
using NSubstitute;
using Synthesis.Bethesda.Execution.GitRepository;
using Synthesis.Bethesda.Execution.Patchers.Git.PrepareDriver;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.Patchers.Git.PrepareDriver
{
    public class GetToLatestMasterTests
    {
        [Theory, SynthAutoData]
        public void MainBranchNullFails(
            IGitRepository repo,
            GetToLatestMaster sut)
        {
            repo.MainBranch.Returns(default(IBranch?));
            sut.TryGet(repo, out _)
                .Should().BeFalse();
        }
        
        [Theory, SynthAutoData]
        public void ReturnsMainBranchName(
            string name,
            IGitRepository repo,
            GetToLatestMaster sut)
        {
            var branch = Substitute.For<IBranch>();
            branch.FriendlyName.Returns(name);
            repo.MainBranch.Returns(branch);
            sut.TryGet(repo, out var outName)
                .Should().BeTrue();
            outName.Should().Be(name);
        }
        
        [Theory, SynthAutoData]
        public void ChecksOutMainBranch(
            IGitRepository repo,
            GetToLatestMaster sut)
        {
            var branch = Substitute.For<IBranch>();
            repo.MainBranch.Returns(branch);
            sut.TryGet(repo, out _);
            repo.Received(1).Checkout(branch);
        }
        
        [Theory, SynthAutoData]
        public void GeneralPipelineOrder(
            IGitRepository repo,
            GetToLatestMaster sut)
        {
            var branch = Substitute.For<IBranch>();
            repo.MainBranch.Returns(branch);
            sut.TryGet(repo, out _);
            Received.InOrder(() =>
            {
                repo.ResetHard();
                repo.Checkout(Arg.Any<IBranch>());
                repo.Pull();
            });
        }
    }
}