using Noggog;
using NSubstitute;
using Synthesis.Bethesda.Execution.CLI;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.Cli
{
    public class ProjectRunProcessStartInfoProviderTests
    {
        [Theory, SynthAutoData]
        public void PassesRunProjectInstruction(
            string path,
            string args,
            ProjectRunProcessStartInfoProvider sut)
        {
            sut.GetStart(path, args);
            sut.CmdStartConstructor.Received(1)
                .Construct("run --project", Arg.Any<FilePath>(), Arg.Any<string[]>());
        }
        
        [Theory, SynthAutoData]
        public void PassesPathAndArgs(
            string path,
            string args,
            ProjectRunProcessStartInfoProvider sut)
        {
            sut.GetStart(path, args);
            sut.CmdStartConstructor.Received(1)
                .Construct(
                    Arg.Any<string>(), 
                    path, 
                    Arg.Is<string[]>(x => x.Contains(args)));
        }
        
        [Theory, SynthAutoData]
        public void PassesExecutionParametersToArgs(
            string path,
            string executionArgs,
            string args,
            ProjectRunProcessStartInfoProvider sut)
        {
            sut.ExecutionParameters.Parameters.Returns(executionArgs);
            sut.GetStart(path, args);
            sut.CmdStartConstructor.Received(1)
                .Construct(
                    Arg.Any<string>(), 
                    Arg.Any<FilePath>(),
                    Arg.Is<string[]>(x => x.Contains(executionArgs)));
        }
        
        [Theory]
        [SynthInlineData(true)]
        [SynthInlineData(false)]
        public void BuildGivenToArgs(
            bool build,
            string path,
            string args,
            ProjectRunProcessStartInfoProvider sut)
        {
            sut.GetStart(path, args, build: build);
            sut.CmdStartConstructor.Received(1)
                .Construct(
                Arg.Any<string>(), 
                    Arg.Any<FilePath>(), 
                    Arg.Is<string[]>(x => x.Contains("--no-build") == !build));
        }
    }
}