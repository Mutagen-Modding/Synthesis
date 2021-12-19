using System.IO;
using System.IO.Abstractions;
using FluentAssertions;
using Mutagen.Bethesda.Environments.DI;
using Noggog;
using Synthesis.Bethesda.Execution.Running.Runner;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.Running.Runner
{
    public class MoveFinalResultsTests
    {
        private readonly string SourcePatchPath = "C:/Workspace/PatcherDir/Synthesis.esp";
        
        private void PrepFileSystem(IFileSystem fs)
        {
            fs.Directory.CreateDirectory("C:/Workspace/PatcherDir/Strings");
            fs.File.WriteAllText(SourcePatchPath, string.Empty);
            fs.File.WriteAllText("C:/Workspace/PatcherDir/Strings/Synthesis_English.STRINGS", string.Empty);
        }
        
        [Theory, SynthAutoData]
        public void CreatesOutputDirectory(
            IFileSystem fs,
            DirectoryPath missingOutputPath,
            MoveFinalResults sut)
        {
            PrepFileSystem(fs);
            sut.Move(SourcePatchPath, missingOutputPath);
            fs.Directory.Exists(missingOutputPath).Should().BeTrue();
        }
        
        [Theory, SynthAutoData]
        public void MovesFilesToWorkspaceFinalDestination(
            IFileSystem fs,
            DirectoryPath missingOutputPath,
            MoveFinalResults sut)
        {
            PrepFileSystem(fs);
            sut.Move(SourcePatchPath, missingOutputPath);
            fs.File.Exists(Path.Combine(missingOutputPath, "Synthesis.esp")).Should().BeTrue();
            fs.File.Exists(Path.Combine(missingOutputPath, "Strings", "Synthesis_English.STRINGS")).Should().BeTrue();
        }
        
        [Theory, SynthAutoData]
        public void MovesFilesToDataDir(
            IFileSystem fs,
            IDataDirectoryProvider dataDirectoryProvider,
            DirectoryPath missingOutputPath,
            MoveFinalResults sut)
        {
            PrepFileSystem(fs);
            sut.Move(SourcePatchPath, missingOutputPath);
            fs.File.Exists(Path.Combine(dataDirectoryProvider.Path, "Synthesis.esp")).Should().BeTrue();
            fs.File.Exists(Path.Combine(dataDirectoryProvider.Path, "Strings", "Synthesis_English.STRINGS")).Should().BeTrue();
        }
        
        [Theory, SynthAutoData]
        public void OverwritesFilesInDataDir(
            IFileSystem fs,
            IDataDirectoryProvider dataDirectoryProvider,
            DirectoryPath missingOutputPath,
            MoveFinalResults sut)
        {
            PrepFileSystem(fs);
            fs.File.WriteAllText(Path.Combine(dataDirectoryProvider.Path, "Synthesis.esp"), "Hello");
            fs.Directory.CreateDirectory(Path.Combine(dataDirectoryProvider.Path, "Strings"));
            fs.File.WriteAllText(Path.Combine(dataDirectoryProvider.Path, "Strings", "Synthesis_English.STRINGS"), "World");
            sut.Move(SourcePatchPath, missingOutputPath);
            fs.File.ReadAllText(Path.Combine(dataDirectoryProvider.Path, "Synthesis.esp")).Should().Be(string.Empty);
            fs.File.ReadAllText(Path.Combine(dataDirectoryProvider.Path, "Strings", "Synthesis_English.STRINGS")).Should().Be(string.Empty);
        }
    }
}