using System.Threading;
using FluentAssertions;
using Noggog;
using NSubstitute;
using Synthesis.Bethesda.Execution.GitRepository;
using Synthesis.Bethesda.Execution.Patchers.Git.Registry;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.Patchers.Git.Registry
{
    public class PrepRegistryRepositoryTests
    {
        [Theory, SynthAutoData]
        public void PassesRegistryUrlToCheckOrClone(
            string url,
            CancellationToken cancel,
            PrepRegistryRepository sut)
        {
            sut.RegistryUrlProvider.Url.Returns(url);
            sut.Prep(cancel);
            sut.CheckOrClone.Received(1).Check(
                url,
                Arg.Any<DirectoryPath>(),
                cancel);
        }
        
        [Theory, SynthAutoData]
        public void PassesRegistryFolderToCheckOrClone(
            DirectoryPath registryFolder,
            CancellationToken cancel,
            PrepRegistryRepository sut)
        {
            sut.RegistryFolderProvider.RegistryFolder.Returns(registryFolder);
            sut.Prep(cancel);
            sut.CheckOrClone.Received(1).Check(
                Arg.Any<GetResponse<string>>(),
                registryFolder,
                cancel);
        }
        
        [Theory, SynthAutoData]
        public void FailedCheckOrCloneReturnsFail(
            GetResponse<RepoPathPair> failed,
            CancellationToken cancel,
            PrepRegistryRepository sut)
        {
            sut.CheckOrClone.Check(default, default, default)
                .ReturnsForAnyArgs(failed);
            sut.Prep(cancel)
                .Succeeded.Should().BeFalse();
        }
        
        [Theory, SynthAutoData]
        public void PassesCheckOrCloneToCheckout(
            string remote,
            DirectoryPath checkoutPath,
            CancellationToken cancel,
            PrepRegistryRepository sut)
        {
            sut.CheckOrClone.Check(default, default, default)
                .ReturnsForAnyArgs(
                    new RepoPathPair(
                        remote,
                        checkoutPath));
            sut.Prep(cancel);
            sut.RepositoryCheckouts.Received(1).Get(checkoutPath);
        }
        
        [Theory, SynthAutoData]
        public void PassesCheckoutToReset(
            IRepositoryCheckout checkout,
            CancellationToken cancel,
            PrepRegistryRepository sut)
        {
            sut.RepositoryCheckouts.Get(default).ReturnsForAnyArgs(checkout);
            sut.Prep(cancel);
            sut.ResetToLatestMain.Received(1).TryReset(checkout.Repository);
        }
        
        [Theory, SynthAutoData]
        public void ReturnsResetResult(
            GetResponse<IBranch> response,
            CancellationToken cancel,
            PrepRegistryRepository sut)
        {
            sut.ResetToLatestMain.TryReset(default!).ReturnsForAnyArgs(response);
            sut.Prep(cancel)
                .Should().Be((ErrorResponse)response);
        }
    }
}