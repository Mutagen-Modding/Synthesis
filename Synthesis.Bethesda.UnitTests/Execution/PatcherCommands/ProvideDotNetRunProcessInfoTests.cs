using System.Diagnostics;
using FluentAssertions;
using NSubstitute;
using Synthesis.Bethesda.Execution.PatcherCommands;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.PatcherCommands
{
    public class ProvideDotNetRunProcessInfoTests
    {
        public record ArgsClass
        {
            private int Field { get; set; }
        }
        
        [Theory, SynthAutoData]
        public void CallsFormatOnArgsObject(
            string path, 
            bool directExe, 
            ArgsClass argsClass,
            bool build,
            RunProcessStartInfoProvider sut)
        {
            sut.GetStart(path, directExe, argsClass, build);
            sut.Format.Received(1).Format(argsClass);
        }
        
        [Theory, SynthAutoData]
        public void DirectExeReturnsProcessStart(
            string path, 
            ArgsClass argsClass,
            bool build,
            string args,
            RunProcessStartInfoProvider sut)
        {
            sut.Format.Format(argsClass).Returns(args);
            var start = sut.GetStart(path, directExe: true, argsClass, build);
            start.FileName.Should().Be(path);
            start.Arguments.Should().Be(args);
        }
        
        [Theory, SynthAutoData]
        public void NonDirectExeCallsProcessStartInfoProvider(
            string path, 
            ArgsClass argsClass,
            bool build,
            string args,
            RunProcessStartInfoProvider sut)
        {
            sut.Format.Format(argsClass).Returns(args);
            sut.GetStart(path, directExe: false, argsClass, build);
            sut.ProjectRunProcessStartInfoProvider.Received(1).GetStart(path, args, build);
        }
        
        [Theory, SynthAutoData]
        public void NonDirectExeReturnsProcessStartInfoProviderResult(
            string path, 
            ArgsClass argsClass,
            bool build,
            string args,
            ProcessStartInfo startInfo,
            RunProcessStartInfoProvider sut)
        {
            sut.ProjectRunProcessStartInfoProvider.GetStart(default!, default!)
                .ReturnsForAnyArgs(startInfo);
            sut.GetStart(path, directExe: false, argsClass, build)
                .Should().Be(startInfo);
        }
    }
}