using System.IO;
using FluentAssertions;
using Noggog;
using NSubstitute;
using Synthesis.Bethesda.Execution.Pathing;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.Pathing
{
    public class WorkingDirectoryProviderTests
    {
        [Theory, SynthAutoData]
        public void CombinesTempDirAndSynthesis(
            DirectoryPath tempDir,
            WorkingDirectoryProvider sut)
        {
            sut.TempDir.Path.Returns(tempDir);
            sut.WorkingDirectory.Should().Be(
                new DirectoryPath(
                    Path.Combine(tempDir, "Synthesis")));
        }
    }
}