using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Noggog;
using NSubstitute;
using Synthesis.Bethesda.Commands;
using Synthesis.Bethesda.Execution;
using Synthesis.Bethesda.Execution.Patchers.Running.Solution;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.Patchers.Running.Solution
{
    public class SolutionPatcherRunTests
    {
        [Theory, SynthAutoData]
        public async Task PrepCallsService(
            CancellationToken cancel,
            SolutionPatcherRun sut)
        {
            await sut.Prep(cancel);
            await sut.PrepService.Received(1).Prep(cancel);
        }

        [Theory, SynthAutoData]
        public async Task ToStringCallsNameProvider(
            SolutionPatcherRun sut)
        {
            sut.NameProvider.ClearReceivedCalls();
            sut.ToString();
            var n = sut.NameProvider.Received(1).Name;
        }

        [Theory, SynthAutoData]
        public async Task PrintsSha(
            RunSynthesisPatcher settings,
            CancellationToken cancel,
            SolutionPatcherRun sut)
        {
            await sut.Run(settings, cancel);
            sut.PrintShaIfApplicable.Received(1).Print();
        }
        
        [Theory, SynthAutoData]
        public async Task RunnabilityCheckCall(
            FilePath projPath,
            RunSynthesisPatcher settings,
            CancellationToken cancel,
            SolutionPatcherRun sut)
        {
            sut.PathToProjProvider.Path.Returns(projPath);
            await sut.Run(settings, cancel);
            await sut.CheckRunnability.Received(1)
                .Check(
                    projPath,
                    directExe: false,
                    loadOrderPath: settings.LoadOrderFilePath,
                    cancel: cancel);
        }
        
        [Theory, SynthAutoData]
        public async Task FailedRunnabilityThrows(
            RunSynthesisPatcher settings,
            CancellationToken cancel,
            ErrorResponse fail,
            SolutionPatcherRun sut)
        {
            sut.CheckRunnability.Check(default!, default, default!, default)
                .ReturnsForAnyArgs(fail);
            var ex = await Assert.ThrowsAsync<CliUnsuccessfulRunException>(async () =>
            {
                await sut.Run(settings, cancel);
            });
            ex.ExitCode.Should().Be((int) Codes.NotRunnable);
            ex.Message.Should().Be(fail.Reason);
        }
        
        [Theory, SynthAutoData]
        public async Task PassesSettingsToRunner(
            RunSynthesisPatcher settings,
            CancellationToken cancel,
            SolutionPatcherRun sut)
        {
            await sut.Run(settings, cancel);
            await sut.SolutionPatcherRunner.Received(1).Run(settings, cancel);
        }
        
        [Theory, SynthAutoData]
        public async Task PipelineOrder(
            RunSynthesisPatcher settings,
            CancellationToken cancel,
            SolutionPatcherRun sut)
        {
            await sut.Run(settings, cancel);
            Received.InOrder(() =>
            {
                sut.CheckRunnability.Check(
                    Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
                sut.SolutionPatcherRunner.Run(Arg.Any<RunSynthesisPatcher>(), Arg.Any<CancellationToken>());
            });
        }
    }
}