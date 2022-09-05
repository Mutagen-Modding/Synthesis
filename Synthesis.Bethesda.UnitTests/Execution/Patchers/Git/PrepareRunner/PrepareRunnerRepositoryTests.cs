using FluentAssertions;
using Noggog;
using NSubstitute;
using Noggog.GitRepository;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Patchers.Git.PrepareRunner;
using Synthesis.Bethesda.Execution.Patchers.Solution;
using Synthesis.Bethesda.Execution.Settings;
using Synthesis.Bethesda.Execution.Versioning;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.Patchers.Git.PrepareRunner;

public class PrepareRunnerRepositoryTests
{
    [Theory, SynthAutoData]
    public async Task CancellationRethrows(
        CheckoutInput checkoutInput,
        CancellationToken cancelledToken,
        PrepareRunnerRepository sut)
    {
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await sut.Checkout(checkoutInput, cancelledToken);
        });
    }
        
    [Theory, SynthAutoData]
    public async Task PassesLocalRepoDirToCheckout(
        DirectoryPath dir,
        CheckoutInput checkoutInput,
        CancellationToken cancel,
        PrepareRunnerRepository sut)
    {
        sut.RunnerRepoDirectoryProvider.Path.Returns(dir);
        await sut.Checkout(checkoutInput, cancel);
        sut.RepoCheckouts.Received(1).Get(dir);
    }
        
    [Theory, SynthAutoData]
    public async Task RepoCheckPasedToResetToTarget(
        DirectoryPath dir,
        IRepositoryCheckout checkout,
        CheckoutInput checkoutInput,
        CancellationToken cancel,
        PrepareRunnerRepository sut)
    {
        sut.RepoCheckouts.Get(dir).ReturnsForAnyArgs(checkout);
        await sut.Checkout(checkoutInput, cancel);
        sut.ResetToTarget.Received(1).Reset(checkout.Repository, checkoutInput.PatcherVersioning, cancel);
    }
        
    [Theory, SynthAutoData]
    public async Task FailedResetToTargetFails(
        CheckoutInput checkoutInput,
        CancellationToken cancel,
        PrepareRunnerRepository sut)
    {
        sut.ResetToTarget.Reset(default!, default!, default)
            .ReturnsForAnyArgs(GetResponse<ResetResults>.Failure);
        var resp = await sut.Checkout(checkoutInput, cancel);
        resp.RunnableState.Succeeded.Should().BeFalse();
        resp.IsHaltingError.Should().BeTrue();
    }
        
    [Theory, SynthAutoData]
    public async Task PassesRepoDirectoryToSolutionFileLocator(
        DirectoryPath dir,
        CheckoutInput checkoutInput,
        CancellationToken cancel,
        PrepareRunnerRepository sut)
    {
        sut.ResetToTarget.Reset(default!, default!, default)
            .ReturnsForAnyArgs(GetResponse<ResetResults>.Succeed(default!));
        sut.RunnerRepoDirectoryProvider.Path.Returns(dir);
            
        await sut.Checkout(checkoutInput, cancel);
        sut.SolutionFileLocator.Received(1).GetPath(dir);
    }
        
    [Theory, SynthAutoData]
    public async Task FailedSolutionFileLocatorFails(
        CheckoutInput checkoutInput,
        CancellationToken cancel,
        PrepareRunnerRepository sut)
    {
        sut.ResetToTarget.Reset(default!, default!, default)
            .ReturnsForAnyArgs(GetResponse<ResetResults>.Succeed(default!));
        sut.SolutionFileLocator.GetPath(default).ReturnsForAnyArgs(default(FilePath?));
            
        var resp = await sut.Checkout(checkoutInput, cancel);
        resp.RunnableState.Succeeded.Should().BeFalse();
        resp.IsHaltingError.Should().BeTrue();
    }
        
    [Theory, SynthAutoData]
    public async Task PassesSolutionToFullProjectPathRetriever(
        FilePath sln,
        CheckoutInput checkoutInput,
        CancellationToken cancel,
        PrepareRunnerRepository sut)
    {
        sut.ResetToTarget.Reset(default!, default!, default)
            .ReturnsForAnyArgs(GetResponse<ResetResults>.Succeed(default!));
        sut.SolutionFileLocator.GetPath(default).ReturnsForAnyArgs(sln);
            
        await sut.Checkout(checkoutInput, cancel);
        sut.RunnerRepoProjectPathRetriever.Received(1).Get(sln, checkoutInput.Proj);
    }
        
    [Theory, SynthAutoData]
    public async Task FailedFullProjectPathFails(
        CheckoutInput checkoutInput,
        CancellationToken cancel,
        PrepareRunnerRepository sut)
    {
        sut.ResetToTarget.Reset(default!, default!, default)
            .ReturnsForAnyArgs(GetResponse<ResetResults>.Succeed(default!));
        sut.SolutionFileLocator.GetPath(default).ReturnsForAnyArgs(new FilePath());
        sut.RunnerRepoProjectPathRetriever.Get(default, default!).ReturnsForAnyArgs(default(ProjectPaths?));
            
        var resp = await sut.Checkout(checkoutInput, cancel);
        resp.RunnableState.Succeeded.Should().BeFalse();
        resp.IsHaltingError.Should().BeTrue();
    }
        
    [Theory, SynthAutoData]
    public async Task RetrievedPathsPassedToModify(
        FilePath sln,
        ProjectPaths proj,
        CheckoutInput checkoutInput,
        CancellationToken cancel,
        PrepareRunnerRepository sut)
    {
        sut.ResetToTarget.Reset(default!, default!, default)
            .ReturnsForAnyArgs(GetResponse<ResetResults>.Succeed(default!));
        sut.SolutionFileLocator.GetPath(default).ReturnsForAnyArgs(sln);
        sut.RunnerRepoProjectPathRetriever.Get(default, default!).ReturnsForAnyArgs(proj);
            
        await sut.Checkout(checkoutInput, cancel);
        sut.ModifyRunnerProjects
            .Received(1)
            .Modify(sln, proj.SubPath, Arg.Any<NugetVersionPair>(), out Arg.Any<NugetVersionPair>());
    }
        
    [Theory, SynthAutoData]
    public async Task ModifyPassedNullIfMatch(
        string vers1,
        string vers2,
        ProjectPaths proj,
        CheckoutInput checkoutInput,
        CancellationToken cancel,
        PrepareRunnerRepository sut)
    {
        sut.ResetToTarget.Reset(default!, default!, default)
            .ReturnsForAnyArgs(GetResponse<ResetResults>.Succeed(default!));
        sut.SolutionFileLocator.GetPath(default).ReturnsForAnyArgs(new FilePath());
        sut.RunnerRepoProjectPathRetriever.Get(default, default!).ReturnsForAnyArgs(proj);
        checkoutInput = checkoutInput with
        {
            LibraryNugets = new NugetsVersioningTarget(
                new NugetVersioningTarget(vers1, NugetVersioningEnum.Match),
                new NugetVersioningTarget(vers2, NugetVersioningEnum.Match))
        };
            
        await sut.Checkout(checkoutInput, cancel);
        sut.ModifyRunnerProjects
            .Received(1)
            .Modify(
                Arg.Any<FilePath>(), 
                Arg.Any<string>(),
                new NugetVersionPair(null, null),
                out Arg.Any<NugetVersionPair>());
    }
        
    [Theory, SynthAutoData]
    public async Task ModifyPassesInputIfNotMatch(
        string vers1,
        string vers2,
        ProjectPaths proj,
        CheckoutInput checkoutInput,
        CancellationToken cancel,
        PrepareRunnerRepository sut)
    {
        sut.ResetToTarget.Reset(default!, default!, default)
            .ReturnsForAnyArgs(GetResponse<ResetResults>.Succeed(default!));
        sut.SolutionFileLocator.GetPath(default).ReturnsForAnyArgs(new FilePath());
        sut.RunnerRepoProjectPathRetriever.Get(default, default!).ReturnsForAnyArgs(proj);
        checkoutInput = checkoutInput with
        {
            LibraryNugets = new NugetsVersioningTarget(
                new NugetVersioningTarget(vers1, NugetVersioningEnum.Latest),
                new NugetVersioningTarget(vers2, NugetVersioningEnum.Latest))
        };
            
        await sut.Checkout(checkoutInput, cancel);
        sut.ModifyRunnerProjects
            .Received(1)
            .Modify(
                Arg.Any<FilePath>(), 
                Arg.Any<string>(),
                new NugetVersionPair(vers1, vers2),
                out Arg.Any<NugetVersionPair>());
    }

    [Theory, SynthAutoData]
    public async Task ReturnsExpectedArgs(
        FilePath sln,
        DirectoryPath runnerDir,
        string vers1,
        string vers2,
        ProjectPaths proj,
        ResetResults resetResults,
        CheckoutInput checkoutInput,
        CancellationToken cancel,
        PrepareRunnerRepository sut)
    {
        sut.ResetToTarget.Reset(default!, default!, default)
            .ReturnsForAnyArgs(GetResponse<ResetResults>.Succeed(resetResults));
        sut.SolutionFileLocator.GetPath(default).ReturnsForAnyArgs(sln);
        sut.RunnerRepoProjectPathRetriever.Get(default, default!).ReturnsForAnyArgs(proj);
        checkoutInput = checkoutInput with
        {
            LibraryNugets = new NugetsVersioningTarget(
                new NugetVersioningTarget(vers1, NugetVersioningEnum.Latest),
                new NugetVersioningTarget(vers2, NugetVersioningEnum.Latest))
        };
            
        var resp = await sut.Checkout(checkoutInput, cancel);
        resp.IsHaltingError.Should().BeFalse();
        resp.RunnableState.Succeeded.Should().BeTrue();
        resp.Item.Project.SolutionPath.Should().Be(sln);
        resp.Item.Project.ProjPath.Should().Be(proj.FullPath);
        resp.Item.Target.Should().Be(resetResults.Target);
        resp.Item.CommitMessage.Should().Be(resetResults.CommitMessage);
        resp.Item.CommitDate.Should().Be(resetResults.CommitDate);
    }

    [Theory, SynthAutoData]
    public async Task BasicPipelineOrder(
        ProjectPaths proj,
        CheckoutInput checkoutInput,
        CancellationToken cancel,
        PrepareRunnerRepository sut)
    {
        sut.ResetToTarget.Reset(default!, default!, default)
            .ReturnsForAnyArgs(GetResponse<ResetResults>.Succeed(default!));
        sut.SolutionFileLocator.GetPath(default)
            .ReturnsForAnyArgs(new FilePath());
        sut.RunnerRepoProjectPathRetriever.Get(default, default!)
            .ReturnsForAnyArgs(proj);
        await sut.Checkout(checkoutInput, cancel);
            
        Received.InOrder(() =>
        {
            sut.RepoCheckouts.Get(Arg.Any<DirectoryPath>());
            sut.ResetToTarget.Reset(
                Arg.Any<IGitRepository>(),
                Arg.Any<GitPatcherVersioning>(),
                Arg.Any<CancellationToken>());
            sut.RunnerRepoProjectPathRetriever.Get(
                Arg.Any<FilePath>(), 
                Arg.Any<FilePath>());
            sut.ModifyRunnerProjects.Modify(
                Arg.Any<FilePath>(),
                Arg.Any<string>(),
                Arg.Any<NugetVersionPair>(),
                out Arg.Any<NugetVersionPair>());
        });
    }
}