using FluentAssertions;
using Noggog;
using NSubstitute;
using Synthesis.Bethesda.Execution.DotNet.ExecutablePath;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.DotNet.ExecutablePath
{
    public class ProcessExecutablePathTests
    {
        [Theory, SynthAutoData(UseMockFileSystem: false)]
        public void NoWorkIfAlreadyExists(
            ProcessExecutablePath sut)
        {
            sut.FileSystem.File.Exists(default).ReturnsForAnyArgs(true);
            sut.Process(default, default);
            var w = sut.WorkingDirectoryProvider.DidNotReceiveWithAnyArgs().WorkingDirectory;
        }
        
        [Theory, SynthAutoData(UseMockFileSystem: false)]
        public void NoWorkIfProjectNotUnderneath(
            FilePath projPath,
            FilePath exePath,
            DirectoryPath missingFolder,
            ProcessExecutablePath sut)
        {
            sut.FileSystem.File.Exists(default).ReturnsForAnyArgs(false);
            sut.WorkingDirectoryProvider.WorkingDirectory.Returns(missingFolder);
            sut.Process(projPath, exePath)
                .Should().Be(exePath);
        }
        
        [Theory, SynthAutoData(UseMockFileSystem: false)]
        public void RebasesToWorkingDirectory(
            ProcessExecutablePath sut)
        {
            var workingDir = @"C:\Users\actual\AppData\Local\Temp";
            var projPath = @"C:\Users\actual\AppData\Local\Temp\Synthesis\Loading\ugqvnbdg.i1q\SomeProj.csproj";
            var junkPath = @"C:\Users\junk\AppData\Local\Temp\Synthesis\Loading\ugqvnbdg.i1q\bin\Debug\net6.0\win-x64\FaceFixer.dll";
            var actualPath = @"C:\Users\actual\AppData\Local\Temp\Synthesis\Loading\ugqvnbdg.i1q\bin\Debug\net6.0\win-x64\FaceFixer.dll";
            sut.FileSystem.File.Exists(default).ReturnsForAnyArgs(false);
            sut.WorkingDirectoryProvider.WorkingDirectory.Returns(new DirectoryPath(workingDir));
            sut.Process(projPath, junkPath)
                .Should().Be(actualPath);
        }
    }
}