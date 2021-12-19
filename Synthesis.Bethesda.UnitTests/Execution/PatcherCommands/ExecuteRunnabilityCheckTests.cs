using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Noggog;
using NSubstitute;
using Synthesis.Bethesda.Commands;
using Synthesis.Bethesda.Execution.PatcherCommands;
using Synthesis.Bethesda.Execution.Patchers.Running.Git;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.PatcherCommands
{
    public class CheckRunnabilityTests
    {
        [Theory, SynthAutoData]
        public async Task DoesNotHaveRunnabilityMetaShortCircuits(
            string path,
            bool directExe,
            string loadOrderPath,
            FilePath buildMetaPath,
            CancellationToken cancel,
            ExecuteRunnabilityCheck sut)
        {
            sut.MetaFileReader.Read(buildMetaPath).Returns(new GitCompilationMeta() { DoesNotHaveRunnability = true });
            await sut.Check(path, directExe, loadOrderPath, buildMetaPath, cancel);
            await sut.ProcessRunner.DidNotReceiveWithAnyArgs().RunWithCallback(default!, default(Action<string>)!, default);
        }
        
        [Theory, SynthAutoData]
        public async Task PassesParametersToGetStart(
            string path,
            bool directExe,
            string loadOrderPath,
            FilePath buildMetaPath,
            CancellationToken cancel,
            ExecuteRunnabilityCheck sut)
        {
            await sut.Check(path, directExe, loadOrderPath, buildMetaPath, cancel);
            sut.RunProcessStartInfoProvider.Received(1).GetStart(path, directExe, new CheckRunnability()
            {
                DataFolderPath = sut.DataDirectoryProvider.Path,
                GameRelease = sut.GameReleaseContext.Release,
                LoadOrderFilePath = loadOrderPath
            });
        }

        [Theory, SynthAutoData]
        public async Task GetStartPassesToRunner(
            string path,
            bool directExe,
            string loadOrderPath,
            FilePath buildMetaPath,
            CancellationToken cancel,
            ProcessStartInfo start,
            ExecuteRunnabilityCheck sut)
        {
            sut.RunProcessStartInfoProvider.GetStart<CheckRunnability>(default!, default, default!)
                .ReturnsForAnyArgs(start);
            await sut.Check(path, directExe, loadOrderPath, buildMetaPath, cancel);
            await sut.ProcessRunner.Received(1).RunWithCallback(
                start,
                Arg.Any<Action<string>>(),
                cancel);
        }

        [Theory, SynthAutoData]
        public async Task NotRunnableResponseFails(
            string path,
            bool directExe,
            string loadOrderPath,
            FilePath buildMetaPath,
            CancellationToken cancel,
            ExecuteRunnabilityCheck sut)
        {
            sut.ProcessRunner.RunWithCallback(default!, default!, default)
                .ReturnsForAnyArgs((int)Codes.NotRunnable);
            (await sut.Check(path, directExe, loadOrderPath, buildMetaPath, cancel))
                .Succeeded.Should().BeFalse();
        }

        [Theory, SynthAutoData]
        public async Task FailedPrintReturnsOnlyMaxLines(
            string path,
            bool directExe,
            string loadOrderPath,
            FilePath buildMetaPath,
            CancellationToken cancel,
            ExecuteRunnabilityCheck sut)
        {
            sut.ProcessRunner.RunWithCallback(
                    Arg.Any<ProcessStartInfo>(),
                    Arg.Do<Action<string>>(x =>
                    {
                        for (int i = 0; i < ExecuteRunnabilityCheck.MaxLines + 5; i++)
                        {
                            x("line");
                        }
                    }),
                    Arg.Any<CancellationToken>())
                .ReturnsForAnyArgs((int)Codes.NotRunnable);
            var resp = await sut.Check(path, directExe, loadOrderPath, buildMetaPath, cancel);
            resp.Reason.Split(Environment.NewLine).Length.Should().Be(ExecuteRunnabilityCheck.MaxLines);
        }

        [Theory, SynthAutoData]
        public async Task AnyOtherResponseSucceeds(
            string path,
            bool directExe,
            string loadOrderPath,
            FilePath buildMetaPath,
            CancellationToken cancel,
            ExecuteRunnabilityCheck sut)
        {
            sut.ProcessRunner.RunWithCallback(default!, default!, default)
                .ReturnsForAnyArgs(((int)Codes.NotRunnable) + 1);
            (await sut.Check(path, directExe, loadOrderPath, buildMetaPath, cancel))
                .Succeeded.Should().BeTrue();
        }
    }
}