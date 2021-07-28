using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using FluentAssertions;
using Noggog.Utility;
using NSubstitute;
using Serilog;
using Synthesis.Bethesda.Execution.Logging;
using Synthesis.Bethesda.Execution.Utility;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution
{
    public class ProcessRunnerTests
    {
        #region Run

        [Theory, SynthAutoData(ConfigureMembers: false)]
        public async Task TypicalRun_StartInfoPassedToFactory(
            [Frozen]ProcessStartInfo startInfo,
            CancellationToken cancel,
            ProcessRunner sut)
        {
            await sut.Run(startInfo, cancel, false);
            sut.Factory.Received(1).Create(startInfo, cancel);
        }

        [Theory, SynthAutoData(ConfigureMembers: false)]
        public async Task TypicalRun_FactoryResultIsRunAndReturned(
            int ret,
            [Frozen]ProcessStartInfo startInfo,
            IProcessWrapper process,
            CancellationToken cancel,
            ProcessRunner sut)
        {
            process.Run().Returns(Task.FromResult(ret));
            sut.Factory.Create(default!).ReturnsForAnyArgs(process);
            (await sut.Run(startInfo, cancel, false))
                .Should().Be(ret);
        }

        #endregion

        #region RunAndCapture

        [Theory, SynthAutoData]
        public async Task RunAndCapture_CallsFactory(
            ProcessStartInfo startInfo,
            CancellationToken cancel,
            ProcessRunner sut)
        {
            await sut.RunAndCapture(startInfo, cancel);
            sut.Factory.Received(1).Create(startInfo, cancel);
        }
        
        [Theory, SynthAutoData(ConfigureMembers: false)]
        public async Task RunAndCapture_PutsOutIntoOut(
            string str,
            [Frozen]ProcessStartInfo startInfo,
            IProcessWrapper process,
            CancellationToken cancel,
            ProcessRunner sut)
        {
            process.Output.Returns(Observable.Return(str));
            sut.Factory.Create(default!).ReturnsForAnyArgs(process);
            var result = await sut.RunAndCapture(startInfo, cancel);
            result.Out.Should().Equal(str);
        }
        
        [Theory, SynthAutoData(ConfigureMembers: false)]
        public async Task RunAndCapture_PutsErrIntoErr(
            string str,
            [Frozen]ProcessStartInfo startInfo,
            IProcessWrapper process,
            CancellationToken cancel,
            ProcessRunner sut)
        {
            process.Error.Returns(Observable.Return(str));
            sut.Factory.Create(default!).ReturnsForAnyArgs(process);
            var result = await sut.RunAndCapture(startInfo, cancel);
            result.Errors.Should().Equal(str);
        }
        
        [Theory, SynthAutoData(ConfigureMembers: false)]
        public async Task RunAndCapture_ReturnsProcessReturn(
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

        #endregion

        #region RunWithCallbacks

        [Theory, SynthAutoData]
        public async Task RunWithCallbacks_CallsFactory(
            ProcessStartInfo startInfo,
            CancellationToken cancel,
            Action<string> callback,
            ProcessRunner sut)
        {
            await sut.Run(startInfo, callback, callback, cancel);
            sut.Factory.Received(1).Create(startInfo, cancel);
        }
        
        [Theory, SynthAutoData(ConfigureMembers: false)]
        public async Task RunWithCallbacks_CallsOutCallback(
            string str,
            [Frozen]ProcessStartInfo startInfo,
            IProcessWrapper process,
            CancellationToken cancel,
            Action<string> errCb,
            ProcessRunner sut)
        {
            process.Output.Returns(Observable.Return(str));
            sut.Factory.Create(default!).ReturnsForAnyArgs(process);
            var received = new List<string>();
            await sut.Run(startInfo, received.Add, errCb, cancel);
            received.Should().Equal(str);
        }
        
        [Theory, SynthAutoData(ConfigureMembers: false)]
        public async Task RunWithCallbacks_PutsErrIntoErr(
            string str,
            [Frozen]ProcessStartInfo startInfo,
            IProcessWrapper process,
            Action<string> outCb,
            CancellationToken cancel,
            ProcessRunner sut)
        {
            process.Error.Returns(Observable.Return(str));
            sut.Factory.Create(default!).ReturnsForAnyArgs(process);
            var received = new List<string>();
            await sut.Run(startInfo, outCb, received.Add, cancel);
            received.Should().Equal(str);
        }
        
        [Theory, SynthAutoData(ConfigureMembers: false)]
        public async Task RunWithCallbacks_ReturnsProcessReturn(
            int ret,
            [Frozen]ProcessStartInfo startInfo,
            IProcessWrapper process,
            Action<string> outCb,
            Action<string> errCb,
            CancellationToken cancel,
            ProcessRunner sut)
        {
            process.Run().Returns(Task.FromResult(ret));
            sut.Factory.Create(default!).ReturnsForAnyArgs(process);
            (await sut.Run(startInfo, outCb, errCb, cancel))
                .Should().Be(ret);
        }

        #endregion

        #region RunWithLogger

        [Theory, SynthAutoData]
        public async Task RunWithLogger_CallsFactory(
            ProcessStartInfo startInfo,
            CancellationToken cancel,
            ProcessRunner sut)
        {
            await sut.Run(startInfo, cancel, true);
            sut.Factory.Received(1).Create(startInfo, cancel);
        }
        
        [Theory, SynthAutoData(ConfigureMembers: false)]
        public async Task RunWithLogger_CallsOutCallback(
            string str,
            [Frozen]ProcessStartInfo startInfo,
            IProcessWrapper process,
            CancellationToken cancel,
            ProcessRunner sut)
        {
            process.Output.Returns(Observable.Return(str));
            sut.Factory.Create(default!).ReturnsForAnyArgs(process);
            await sut.Run(startInfo, cancel, true);
            sut.Logger.Received(1).Information(str);
        }
        
        [Theory, SynthAutoData(ConfigureMembers: false)]
        public async Task RunWithLogger_PutsErrIntoErr(
            string str,
            [Frozen]ProcessStartInfo startInfo,
            IProcessWrapper process,
            CancellationToken cancel,
            ProcessRunner sut)
        {
            process.Error.Returns(Observable.Return(str));
            sut.Factory.Create(default!).ReturnsForAnyArgs(process);
            await sut.Run(startInfo, cancel, true);
            sut.Logger.Received(1).Error(str);
        }
        
        [Theory, SynthAutoData(ConfigureMembers: false)]
        public async Task RunWithLogger_ReturnsProcessReturn(
            int ret,
            [Frozen]ProcessStartInfo startInfo,
            IProcessWrapper process,
            CancellationToken cancel,
            ProcessRunner sut)
        {
            process.Run().Returns(Task.FromResult(ret));
            sut.Factory.Create(default!).ReturnsForAnyArgs(process);
            (await sut.Run(startInfo, cancel, true))
                .Should().Be(ret);
        }

        #endregion
    }
}