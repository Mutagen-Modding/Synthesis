using System.IO;
using FluentAssertions;
using Noggog;
using NSubstitute;
using Synthesis.Bethesda.Execution.Profile;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.Profile
{
    public class ProfileDirectoriesTests
    {
        [Theory, SynthAutoData]
        public void ProfileDirectory(
            string id,
            DirectoryPath workingDir,
            ProfileDirectories sut)
        {
            sut.Ident.ID.Returns(id);
            sut.Paths.WorkingDirectory.Returns(workingDir);
            sut.ProfileDirectory.Should().Be(
                new DirectoryPath(
                    Path.Combine(workingDir, id)));
        }
        
        [Theory, SynthAutoData]
        public void WorkingDirectoryPassesIdToSubPathProvider(
            string id,
            ProfileDirectories sut)
        {
            sut.Ident.ID.Returns(id);
            var dir = sut.WorkingDirectory;
            sut.WorkingDirectorySubPaths.Received(1).ProfileWorkingDirectory(id);
        }
        
        [Theory, SynthAutoData]
        public void WorkingDirectoryReturnsSubPathsProviderResult(
            DirectoryPath workingDir,
            ProfileDirectories sut)
        {
            sut.WorkingDirectorySubPaths.ProfileWorkingDirectory(default!)
                .ReturnsForAnyArgs(workingDir);
            sut.WorkingDirectory.Should().Be(workingDir);
        }
    }
}