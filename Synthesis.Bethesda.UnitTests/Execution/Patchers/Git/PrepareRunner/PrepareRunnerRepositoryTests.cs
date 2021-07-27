using System;
using System.Threading;
using System.Threading.Tasks;
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
    }
}