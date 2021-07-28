using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Noggog;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Synthesis.Bethesda.Execution.GitRepository;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Patchers.Git.PrepareRunner;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.Patchers.Git.PrepareRunner
{
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
    }
}