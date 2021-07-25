using System.Diagnostics;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using FluentAssertions;
using Noggog.Utility;
using NSubstitute;
using Synthesis.Bethesda.Execution;
using Synthesis.Bethesda.Execution.Utility;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution
{
    public class ProcessRunnerTests
    {
        [Theory, SynthAutoData]
        public void CallsFactory(
            ProcessStartInfo startInfo,
            CancellationToken cancel,
            ProcessRunner sut)
        {
            sut.RunAndCapture(startInfo, cancel);
            sut.Factory.Received(1).Create(startInfo, cancel);
        }
        
        [Theory, SynthAutoData]
        public void PutsOutIntoOut(
            string str,
            [Frozen]ProcessStartInfo startInfo,
            IProcessWrapper process,
            CancellationToken cancel,
            ProcessRunner sut)
        {
            process.Output.Returns(Observable.Return(str));
            sut.Factory.Create(default!).ReturnsForAnyArgs(process);
            sut.RunAndCapture(startInfo, cancel)
                .Result.Out.Should().Equal(str);
        }
        
        [Theory, SynthAutoData]
        public void PutsErrIntoErr(
            string str,
            [Frozen]ProcessStartInfo startInfo,
            IProcessWrapper process,
            CancellationToken cancel,
            ProcessRunner sut)
        {
            process.Error.Returns(Observable.Return(str));
            sut.Factory.Create(default!).ReturnsForAnyArgs(process);
            sut.RunAndCapture(startInfo, cancel)
                .Result.Errors.Should().Equal(str);
        }
        
        [Theory, SynthAutoData]
        public async Task ReturnsProcessReturn(
            int ret,
            [Frozen]ProcessStartInfo startInfo,
            IProcessWrapper process,
            CancellationToken cancel,
            ProcessRunner sut)
        {
            process.Run().Returns(Task.FromResult(ret));
            sut.Factory.Create(default!).ReturnsForAnyArgs(process);
            (await sut.RunAndCapture(startInfo, cancel))
                .Result.Should().Be(ret);
        }

        [Theory, SynthAutoData]
        public async Task TypicalRun(
            int ret,
            [Frozen]ProcessStartInfo startInfo,
            IProcessWrapper process,
            CancellationToken cancel,
            ProcessRunner sut)
        {
            process.Run().Returns(Task.FromResult(ret));
            sut.Factory.Create(default!).ReturnsForAnyArgs(process);
            (await sut.Run(startInfo, cancel))
                .Should().Be(ret);
        }
    }
}