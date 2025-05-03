using AutoFixture.Xunit2;
using Shouldly;
using Noggog;
using NSubstitute;
using Noggog.GitRepository;
using Noggog.Testing.Extensions;
using Synthesis.Bethesda.Execution.Patchers.Git.Services.PrepareDriver;
using Synthesis.Bethesda.UnitTests.AutoData;

namespace Synthesis.Bethesda.UnitTests.Execution.Patchers.Git.PrepareDriver;

public class RetrieveRepoVersioningPointsTests
{
    [Theory, SynthAutoData]
    public void RetrievesAllTags(
        ITag[] repoTags,
        [Frozen]IGitRepository repo,
        RetrieveRepoVersioningPoints sut)
    {
        repo.Tags.Returns(repoTags);
        sut.Retrieve(repo, out var tags, out _);
        tags.ShouldEqual(
            repoTags.Select((t, i) => new DriverTag(i, t.FriendlyName, t.Sha)));
    }
        
    [Theory, SynthAutoData(ConfigureMembers: true)]
    public void RetrievesBranches(
        IBranch[] repoBranches,
        [Frozen]IGitRepository repo,
        RetrieveRepoVersioningPoints sut)
    {
        repo.Branches.Returns(repoBranches);
        sut.Retrieve(repo, out _, out var branches);
        ((IEnumerable<KeyValuePair<string, string>>)branches).ShouldEqual(
            repoBranches.Select((t) => new KeyValuePair<string, string>(t.FriendlyName, t.Tip.Sha)));
    }
        
    [Theory, SynthAutoData(ConfigureMembers: true)]
    public void BranchCollisionThrows(
        [Frozen]IGitRepository repo,
        RetrieveRepoVersioningPoints sut)
    {
        var b1 = Substitute.For<IBranch>();
        b1.FriendlyName.Returns("HELLO");
        var b2 = Substitute.For<IBranch>();
        b2.FriendlyName.Returns("hello");
        repo.Branches.Returns(b1.AsEnumerable().And(b2));
        Assert.Throws<ArgumentException>(() =>
        {
            sut.Retrieve(repo, out _, out _);
        });
    }
}