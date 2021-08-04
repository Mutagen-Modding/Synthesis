using System.IO;
using Mutagen.Bethesda.Plugins;
using NSubstitute;
using Synthesis.Bethesda.Execution.Running.Runner;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.Running.Runner
{
    public class MoveFinalResultsTests
    {
        [Theory, SynthAutoData]
        public void ThrowsIfFinalPatchMissing(
            ModPath missingFinalPatch,
            ModPath existingOutput,
            MoveFinalResults sut)
        {
            Assert.Throws<FileNotFoundException>(() =>
            {
                sut.Move(missingFinalPatch, existingOutput);
            });
        }
        
        [Theory, SynthAutoData(UseMockFileSystem: false)]
        public void DeleteFileIfExists(
            ModPath existingFinalPatch,
            ModPath existingOutput,
            MoveFinalResults sut)
        {
            sut.FileSystem.File.Exists(existingFinalPatch).Returns(true);
            sut.FileSystem.File.Exists(existingOutput).Returns(true);
            sut.Move(existingFinalPatch, existingOutput);
            sut.FileSystem.File.Received(1).Delete(existingOutput);
        }
        
        [Theory, SynthAutoData(UseMockFileSystem: false)]
        public void DoesNotCallDeleteFileIfMissing(
            ModPath existingFinalPatch,
            ModPath missingOutput,
            MoveFinalResults sut)
        {
            sut.FileSystem.File.Exists(existingFinalPatch).Returns(true);
            sut.FileSystem.File.Exists(missingOutput).Returns(false);
            sut.Move(existingFinalPatch, missingOutput);
            sut.FileSystem.File.DidNotReceiveWithAnyArgs().Delete(missingOutput);
        }
        
        [Theory, SynthAutoData(UseMockFileSystem: false)]
        public void CallsCopy(
            ModPath existingFinalPatch,
            ModPath outputPath,
            MoveFinalResults sut)
        {
            sut.FileSystem.File.Exists(existingFinalPatch).Returns(true);
            sut.FileSystem.File.Exists(outputPath).Returns(false);
            sut.Move(existingFinalPatch, outputPath);
            sut.FileSystem.File.Received(1).Copy(existingFinalPatch.Path, outputPath.Path);
        }
        
        [Theory, SynthAutoData(UseMockFileSystem: false)]
        public void CreatesDirectoryIfMissing(
            ModPath existingFinalPatch,
            ModPath outputPath,
            MoveFinalResults sut)
        {
            sut.FileSystem.File.Exists(existingFinalPatch).Returns(true);
            sut.FileSystem.Directory.Exists(outputPath.Path.Directory).Returns(false);
            sut.FileSystem.File.Exists(outputPath).Returns(false);
            sut.Move(existingFinalPatch, outputPath);
            sut.FileSystem.Directory.Received(1).CreateDirectory(outputPath.Path.Directory);
        }
        
        [Theory, SynthAutoData(UseMockFileSystem: false)]
        public void DoesNotCreateDirectoryIfExists(
            ModPath existingFinalPatch,
            ModPath outputPath,
            MoveFinalResults sut)
        {
            sut.FileSystem.File.Exists(existingFinalPatch).Returns(true);
            sut.FileSystem.Directory.Exists(outputPath.Path.Directory).Returns(true);
            sut.FileSystem.File.Exists(outputPath).Returns(false);
            sut.Move(existingFinalPatch, outputPath);
            sut.FileSystem.Directory.DidNotReceiveWithAnyArgs().CreateDirectory(outputPath.Path.Directory);
        }
    }
}