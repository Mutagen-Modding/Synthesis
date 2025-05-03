using Shouldly;
using Noggog;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Noggog.GitRepository;
using Noggog.Testing.Extensions;
using Synthesis.Bethesda.Execution.Patchers.Git.Services.PrepareDriver;
using Synthesis.Bethesda.UnitTests.AutoData;

namespace Synthesis.Bethesda.UnitTests.Execution.Patchers.Git.PrepareDriver;

public class PrepareDriverRepositoryTests
{
    [Theory, SynthAutoData]
    public void PassDriverPathToClone(
        GetResponse<string> remotePath,
        CancellationToken cancel,
        PrepareDriverRespository sut)
    {
        sut.Prepare(remotePath, cancel);
        sut.CheckOrClone.Received(1).Check(
            remotePath,
            sut.DriverRepoDirectoryProvider.Path,
            Arg.Any<Func<IGitRepository, ErrorResponse>?>(),
            cancel);
    }
        
    [Theory, SynthAutoData]
    public void FailedCloneFails(
        GetResponse<string> remotePath,
        CancellationToken cancel,
        PrepareDriverRespository sut)
    {
        sut.CheckOrClone.Check(default, default, default)
            .ReturnsForAnyArgs(GetResponse<RepoPathPair>.Failure);
        sut.Prepare(remotePath, cancel)
            .Succeeded.ShouldBeFalse();
    }
        
    [Theory, SynthAutoData]
    public void PassesDriverRepoPathToCheckout(
        DirectoryPath localPath,
        RepoPathPair repoPathPair,
        GetResponse<string> remotePath,
        CancellationToken cancel,
        PrepareDriverRespository sut)
    {
        sut.DriverRepoDirectoryProvider.Path.Returns(localPath);
        sut.CheckOrClone.Check(default, default, default)
            .ReturnsForAnyArgs(GetResponse<RepoPathPair>.Succeed(repoPathPair));
        sut.Prepare(remotePath, cancel);
        sut.RepoCheckouts.Received(1).Get(localPath);
    }
        
    [Theory, SynthAutoData]
    public void PassesCheckoutToGetLatestMaster(
        IRepositoryCheckout checkout,
        RepoPathPair repoPathPair,
        GetResponse<string> remotePath,
        CancellationToken cancel,
        PrepareDriverRespository sut)
    {
        sut.RepoCheckouts.Get(default).ReturnsForAnyArgs(checkout);
        sut.CheckOrClone.Check(default, default, default)
            .ReturnsForAnyArgs(GetResponse<RepoPathPair>.Succeed(repoPathPair));
        sut.Prepare(remotePath, cancel);
        sut.ResetToLatestMain.Received(1).TryReset(checkout.Repository);
    }
        
    [Theory, SynthAutoData]
    public void FailedGetToMasterFails(
        RepoPathPair repoPathPair,
        GetResponse<string> remotePath,
        CancellationToken cancel,
        PrepareDriverRespository sut)
    {
        sut.ResetToLatestMain.TryReset(default!).ReturnsForAnyArgs(GetResponse<IBranch>.Failure);
        sut.CheckOrClone.Check(default, default, default)
            .ReturnsForAnyArgs(GetResponse<RepoPathPair>.Succeed(repoPathPair));
        sut.Prepare(remotePath, cancel)
            .Succeeded.ShouldBeFalse();
    }
        
    [Theory, SynthAutoData]
    public void ThrowingGetToMasterFails(
        RepoPathPair repoPathPair,
        GetResponse<string> remotePath,
        CancellationToken cancel,
        PrepareDriverRespository sut)
    {
        sut.ResetToLatestMain.TryReset(default!).ThrowsForAnyArgs<NotImplementedException>();
        sut.CheckOrClone.Check(default, default, default)
            .ReturnsForAnyArgs(GetResponse<RepoPathPair>.Succeed(repoPathPair));
        sut.Prepare(remotePath, cancel)
            .Succeeded.ShouldBeFalse();
    }
        
    [Theory, SynthAutoData]
    public void PassesCheckoutToRetrieveVersioningPoints(
        IRepositoryCheckout checkout,
        RepoPathPair repoPathPair,
        GetResponse<string> remotePath,
        CancellationToken cancel,
        PrepareDriverRespository sut)
    {
        sut.RepoCheckouts.Get(default).ReturnsForAnyArgs(checkout);
        sut.CheckOrClone.Check(default, default, default)
            .ReturnsForAnyArgs(GetResponse<RepoPathPair>.Succeed(repoPathPair));
        sut.Prepare(remotePath, cancel);
        sut.RetrieveRepoVersioningPoints.Received(1)
            .Retrieve(
                checkout.Repository, 
                out Arg.Any<List<DriverTag>>(),
                out Arg.Any<Dictionary<string, string>>());
    }
        
    [Theory, SynthAutoData]
    public void ThrowingRetrieveVersioningPointsFails(
        RepoPathPair repoPathPair,
        GetResponse<string> remotePath,
        CancellationToken cancel,
        PrepareDriverRespository sut)
    {
        sut.RetrieveRepoVersioningPoints.When(x => x.Retrieve(
                Arg.Any<IGitRepository>(), 
                out Arg.Any<List<DriverTag>>(),
                out Arg.Any<Dictionary<string, string>>()))
            .Do(x => throw new NotImplementedException());
        sut.CheckOrClone.Check(default, default, default)
            .ReturnsForAnyArgs(GetResponse<RepoPathPair>.Succeed(repoPathPair));
        sut.Prepare(remotePath, cancel)
            .Succeeded.ShouldBeFalse();
    }
        
    [Theory, SynthAutoData]
    public void FailedGetDriverPathsFails(
        RepoPathPair repoPathPair,
        GetResponse<string> remotePath,
        CancellationToken cancel,
        PrepareDriverRespository sut)
    {
        sut.GetDriverPaths.Get().Returns(GetResponse<DriverPaths>.Failure);
        sut.CheckOrClone.Check(default, default, default)
            .ReturnsForAnyArgs(GetResponse<RepoPathPair>.Succeed(repoPathPair));
        sut.Prepare(remotePath, cancel)
            .Succeeded.ShouldBeFalse();
    }
        
    [Theory, SynthAutoData]
    public void ExpectedReturn(
        RepoPathPair repoPathPair,
        IBranch masterBranch,
        FilePath slnPath,
        List<string> availableProjs,
        GetResponse<string> remotePath,
        List<DriverTag> tags,
        Dictionary<string, string> branchShas,
        CancellationToken cancel,
        PrepareDriverRespository sut)
    {
        sut.ResetToLatestMain.TryReset(default!)
            .ReturnsForAnyArgs(GetResponse<IBranch>.Succeed(masterBranch));
        sut.RetrieveRepoVersioningPoints.When(x => x.Retrieve(
                Arg.Any<IGitRepository>(),
                out Arg.Any<List<DriverTag>>(),
                out Arg.Any<Dictionary<string, string>>()))
            .Do(x =>
            {
                x[1] = tags;
                x[2] = branchShas;
            });
        sut.GetDriverPaths.Get().ReturnsForAnyArgs(
            GetResponse<DriverPaths>.Succeed(
                new DriverPaths(slnPath, availableProjs)));
        sut.CheckOrClone.Check(default, default, default)
            .ReturnsForAnyArgs(GetResponse<RepoPathPair>.Succeed(repoPathPair));
        var ret = sut.Prepare(remotePath, cancel);
        ret.Succeeded.ShouldBeTrue();
        ret.Value.Tags.ShouldBe(tags);
        ret.Value.AvailableProjects.ShouldEqual(availableProjs);
        ret.Value.BranchShas.ShouldBe(branchShas);
        ret.Value.SolutionPath.ShouldBe(slnPath);
        ret.Value.MasterBranchName.ShouldBe(masterBranch.FriendlyName);
    }
        
    [Theory, SynthAutoData]
    public void PipelineOrder(
        RepoPathPair repoPathPair,
        GetResponse<string> remotePath,
        CancellationToken cancel,
        PrepareDriverRespository sut)
    {
        sut.CheckOrClone.Check(default, default, default)
            .ReturnsForAnyArgs(GetResponse<RepoPathPair>.Succeed(repoPathPair));
        sut.Prepare(remotePath, cancel);
        Received.InOrder(() =>
        {
            sut.CheckOrClone.Check(
                Arg.Any<GetResponse<string>>(),
                Arg.Any<DirectoryPath>(),
                Arg.Any<Func<IGitRepository, ErrorResponse>?>(),
                Arg.Any<CancellationToken>());
            sut.RepoCheckouts.Get(Arg.Any<DirectoryPath>());
            sut.ResetToLatestMain.TryReset(
                Arg.Any<IGitRepository>());
            sut.RetrieveRepoVersioningPoints.Retrieve(
                Arg.Any<IGitRepository>(),
                out Arg.Any<List<DriverTag>>(), 
                out Arg.Any<Dictionary<string, string>>());
            sut.GetDriverPaths.Get();
        });
        Received.InOrder(() =>
        {
            sut.ResetToLatestMain.TryReset(
                Arg.Any<IGitRepository>());
            sut.RetrieveRepoVersioningPoints.Retrieve(
                Arg.Any<IGitRepository>(),
                out Arg.Any<List<DriverTag>>(), 
                out Arg.Any<Dictionary<string, string>>());
        });
        Received.InOrder(() =>
        {
            sut.ResetToLatestMain.TryReset(
                Arg.Any<IGitRepository>());
            sut.GetDriverPaths.Get();
        });
    }
}