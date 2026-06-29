using System.IO.Abstractions;
using AutoFixture.Xunit2;
using Shouldly;
using Mutagen.Bethesda.Plugins;
using Noggog.Testing.AutoFixture;
using NSubstitute;
using Synthesis.Bethesda.Execution.Patchers.Running;
using Synthesis.Bethesda.Execution.Reporters;
using Synthesis.Bethesda.Execution.Running.Runner;
using Synthesis.Bethesda.UnitTests.AutoData;

namespace Synthesis.Bethesda.UnitTests.Execution.Running.Runner;

public class FinalizePatcherRunTests
{
    [Theory, SynthAutoData]
    public async Task OutputFileMissingReturnsNull(
        IPatcherPrepAndRun patcher,
        ModPath missingOutput,
        FinalizePatcherRun sut)
    {
        sut.Finalize(patcher, missingOutput)
            .ShouldBeNull();
    }

    [Theory, SynthAutoData]
    public async Task OutputFileExistsReturnsOutputPath(
        IPatcherPrepAndRun patcher,
        ModPath existingOutput,
        FinalizePatcherRun sut)
    {
        sut.Finalize(patcher, existingOutput)
            .ShouldBe(existingOutput.Path);
    }

    [Theory]
    [SynthCustomInlineData(FileSystem: TargetFileSystem.Substitute)]
    public void NoOutputAndNoSplitFilesReturnsNull(
        IPatcherPrepAndRun patcher,
        IRunReporter reporter,
        [Frozen] IFileSystem fileSystem,
        FinalizePatcherRun sut)
    {
        var outputDir = @"C:\Output";
        var outputModKey = ModKey.FromFileName("Patch.esp");
        var outputPath = Path.Combine(outputDir, outputModKey.FileName);

        // Neither main file nor split files exist
        fileSystem.File.Exists(outputPath).Returns(false);
        fileSystem.Directory.Exists(outputDir).Returns(true);
        fileSystem.Directory.EnumerateFiles(outputDir, "Patch_*.esp", SearchOption.TopDirectoryOnly)
            .Returns(Array.Empty<string>());

        var result = sut.Finalize(patcher, outputPath);

        result.ShouldBeNull();
    }
}