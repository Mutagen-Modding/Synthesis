using FluentAssertions;
using Noggog;
using NSubstitute;
using Synthesis.Bethesda.Execution.Running.Runner;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.Running.Runner
{
    public class RunLoadOrderPathProviderTests
    {
        [Theory, SynthAutoData]
        public void PathReturnsUnderWorkingDirectory(
            DirectoryPath workingDirectory,
            RunLoadOrderPathProvider sut)
        {
            sut.ProfileDirectories.WorkingDirectory.Returns(workingDirectory);
            sut.Path.IsUnderneath(workingDirectory).Should().BeTrue();
        }
    }
}