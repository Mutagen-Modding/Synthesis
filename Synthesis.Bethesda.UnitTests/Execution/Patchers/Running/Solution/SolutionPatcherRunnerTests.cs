using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Mutagen.Bethesda.Synthesis.CLI;
using Noggog;
using NSubstitute;
using Synthesis.Bethesda.Commands;
using Synthesis.Bethesda.Execution;
using Synthesis.Bethesda.Execution.Patchers.Running.Solution;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.Patchers.Running.Solution
{
    public class SolutionPatcherRunnerTests
    {
        [Theory, SynthAutoData]
        public async Task PassesSettingsToArgConstructor(
            RunSynthesisPatcher settings, 
            CancellationToken cancel,
            SolutionPatcherRunner sut)
        {
            sut.ProcessRunner.Run(default!, default).ReturnsForAnyArgs(0);
            await sut.Run(settings, cancel);
            sut.ConstructArgs.Received(1).Construct(settings);
        }
        
        [Theory, SynthAutoData]
        public async Task PassesArgsToFormatter(
            RunSynthesisPatcher settings, 
            CancellationToken cancel,
            RunSynthesisMutagenPatcher args,
            SolutionPatcherRunner sut)
        {
            sut.ProcessRunner.Run(default!, default).ReturnsForAnyArgs(0);
            sut.ConstructArgs.Construct(default!).ReturnsForAnyArgs(args);
            await sut.Run(settings, cancel);
            sut.Formatter.Received(1).Format(args);
        }
        
        [Theory, SynthAutoData]
        public async Task PassesArgsAndPathToCommandStartInfoConstructor(
            RunSynthesisPatcher settings, 
            CancellationToken cancel,
            string formattedArgs,
            FilePath projPath,
            SolutionPatcherRunner sut)
        {
            sut.ProcessRunner.Run(default!, default).ReturnsForAnyArgs(0);
            sut.PathToProjProvider.Path.Returns(projPath);
            sut.Formatter.Format<RunSynthesisMutagenPatcher>(default!).ReturnsForAnyArgs(formattedArgs);
            await sut.Run(settings, cancel);
            sut.ProcessRunStartInfoProvider.Received(1)
                .GetStart(
                    projPath,
                    formattedArgs,
                    build: false);
        }
        
        [Theory, SynthAutoData]
        public async Task StartInfoPassedToRunner(
            RunSynthesisPatcher settings, 
            CancellationToken cancel,
            ProcessStartInfo startInfo,
            SolutionPatcherRunner sut)
        {
            sut.ProcessRunner.Run(default!, default).ReturnsForAnyArgs(0);
            sut.ProcessRunStartInfoProvider.GetStart(default!, default!)
                .ReturnsForAnyArgs(startInfo);
            await sut.Run(settings, cancel);
            await sut.ProcessRunner.Received(1).Run(startInfo, cancel);
        }
        
        [Theory, SynthAutoData]
        public async Task BadRunResultThrows(
            RunSynthesisPatcher settings, 
            CancellationToken cancel,
            SolutionPatcherRunner sut)
        {
            sut.ProcessRunner.Run(default!, default).ReturnsForAnyArgs(-1);
            var ex = await Assert.ThrowsAsync<CliUnsuccessfulRunException>(async () =>
            {
                await sut.Run(settings, cancel);
            });
            ex.ExitCode.Should().Be(-1);
        }
    }
}