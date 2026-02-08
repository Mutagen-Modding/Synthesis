using System.IO.Abstractions;
using Mutagen.Bethesda.Plugins.IO.DI;
using NSubstitute;
using Shouldly;
using Noggog;
using Synthesis.Bethesda.Execution.Profile.Services;
using Synthesis.Bethesda.Execution.Reporters;
using Synthesis.Bethesda.Execution.Running.Runner;
using Synthesis.Bethesda.UnitTests.AutoData;

namespace Synthesis.Bethesda.UnitTests.Execution.Running.Runner;

public class MoveFinalResultsTests
{
    private const string SourceDir = "C:/Workspace/PatcherDir";
    private const string SourcePatchPath = "C:/Workspace/PatcherDir/Synthesis.esp";

    private static MoveFinalResults CreateSut(
        IFileSystem fs,
        DirectoryPath workspaceOutput)
    {
        var associatedFilesLocator = new AssociatedFilesLocator(fs);
        var modFilesMover = new ModFilesMover(fs, associatedFilesLocator);
        var profileDirectories = Substitute.For<IProfileDirectories>();
        profileDirectories.OutputDirectory.Returns(workspaceOutput);
        var reporter = Substitute.For<IRunReporter>();

        return new MoveFinalResults(
            reporter,
            profileDirectories,
            fs,
            modFilesMover,
            associatedFilesLocator);
    }

    private static void PrepFileSystem(IFileSystem fs)
    {
        fs.Directory.CreateDirectory($"{SourceDir}/Strings");
        fs.File.WriteAllText(SourcePatchPath, string.Empty);
        fs.File.WriteAllText($"{SourceDir}/Strings/Synthesis_English.STRINGS", string.Empty);
    }

    [Theory, SynthAutoData]
    public void CreatesOutputDirectory(
        IFileSystem fs,
        DirectoryPath workspaceOutput,
        DirectoryPath missingOutputPath)
    {
        var sut = CreateSut(fs, workspaceOutput);
        PrepFileSystem(fs);
        sut.Move(SourcePatchPath, missingOutputPath);
        fs.Directory.Exists(missingOutputPath).ShouldBeTrue();
    }

    [Theory, SynthAutoData]
    public void MovesFilesToFinalDestination(
        IFileSystem fs,
        DirectoryPath workspaceOutput,
        DirectoryPath outputPath)
    {
        var sut = CreateSut(fs, workspaceOutput);
        PrepFileSystem(fs);
        sut.Move(SourcePatchPath, outputPath);
        fs.File.Exists(Path.Combine(outputPath, "Synthesis.esp")).ShouldBeTrue();
        fs.File.Exists(Path.Combine(outputPath, "Strings", "Synthesis_English.STRINGS")).ShouldBeTrue();
    }

    [Theory, SynthAutoData]
    public void MovesFilesToWorkspaceDir(
        IFileSystem fs,
        DirectoryPath workspaceOutput,
        DirectoryPath outputPath)
    {
        var sut = CreateSut(fs, workspaceOutput);
        PrepFileSystem(fs);
        sut.Move(SourcePatchPath, outputPath);
        fs.File.Exists(Path.Combine(workspaceOutput, "Synthesis.esp")).ShouldBeTrue();
        fs.File.Exists(Path.Combine(workspaceOutput, "Strings", "Synthesis_English.STRINGS")).ShouldBeTrue();
    }

    [Theory, SynthAutoData]
    public void OverwritesFilesInFinalDestination(
        IFileSystem fs,
        DirectoryPath workspaceOutput,
        DirectoryPath existingOutputDir)
    {
        var sut = CreateSut(fs, workspaceOutput);
        PrepFileSystem(fs);
        fs.File.WriteAllText(Path.Combine(existingOutputDir.Path, "Synthesis.esp"), "Hello");
        fs.Directory.CreateDirectory(Path.Combine(existingOutputDir.Path, "Strings"));
        fs.File.WriteAllText(Path.Combine(existingOutputDir.Path, "Strings", "Synthesis_English.STRINGS"), "World");
        sut.Move(SourcePatchPath, existingOutputDir);
        fs.File.ReadAllText(Path.Combine(existingOutputDir.Path, "Synthesis.esp")).ShouldBe(string.Empty);
        fs.File.ReadAllText(Path.Combine(existingOutputDir.Path, "Strings", "Synthesis_English.STRINGS")).ShouldBe(string.Empty);
    }

    [Theory, SynthAutoData]
    public void CopiesSplitPluginFiles(
        IFileSystem fs,
        DirectoryPath workspaceOutput,
        DirectoryPath outputPath)
    {
        var sut = CreateSut(fs, workspaceOutput);
        fs.Directory.CreateDirectory(SourceDir);
        fs.File.WriteAllText(SourcePatchPath, "base");
        fs.File.WriteAllText($"{SourceDir}/Synthesis_2.esp", "split2");
        fs.File.WriteAllText($"{SourceDir}/Synthesis_3.esp", "split3");

        sut.Move(SourcePatchPath, outputPath);

        fs.File.ReadAllText(Path.Combine(outputPath, "Synthesis.esp")).ShouldBe("base");
        fs.File.ReadAllText(Path.Combine(outputPath, "Synthesis_2.esp")).ShouldBe("split2");
        fs.File.ReadAllText(Path.Combine(outputPath, "Synthesis_3.esp")).ShouldBe("split3");
    }

    [Theory, SynthAutoData]
    public void CleansStaleSplitPluginFilesAtDestination(
        IFileSystem fs,
        DirectoryPath workspaceOutput,
        DirectoryPath outputPath)
    {
        var sut = CreateSut(fs, workspaceOutput);

        // Source: base + _2 only
        fs.Directory.CreateDirectory(SourceDir);
        fs.File.WriteAllText(SourcePatchPath, "base");
        fs.File.WriteAllText($"{SourceDir}/Synthesis_2.esp", "split2");

        // Destination: base + _2 + _3 + _4 (stale)
        fs.Directory.CreateDirectory(outputPath);
        fs.File.WriteAllText(Path.Combine(outputPath, "Synthesis.esp"), "old");
        fs.File.WriteAllText(Path.Combine(outputPath, "Synthesis_2.esp"), "old");
        fs.File.WriteAllText(Path.Combine(outputPath, "Synthesis_3.esp"), "old");
        fs.File.WriteAllText(Path.Combine(outputPath, "Synthesis_4.esp"), "old");

        sut.Move(SourcePatchPath, outputPath);

        // New content should be there
        fs.File.ReadAllText(Path.Combine(outputPath, "Synthesis.esp")).ShouldBe("base");
        fs.File.ReadAllText(Path.Combine(outputPath, "Synthesis_2.esp")).ShouldBe("split2");

        // Stale split files should be cleaned up
        fs.File.Exists(Path.Combine(outputPath, "Synthesis_3.esp")).ShouldBeFalse();
        fs.File.Exists(Path.Combine(outputPath, "Synthesis_4.esp")).ShouldBeFalse();
    }

    [Theory, SynthAutoData]
    public void NonSplitModCleansAllStaleSplitFiles(
        IFileSystem fs,
        DirectoryPath workspaceOutput,
        DirectoryPath outputPath)
    {
        var sut = CreateSut(fs, workspaceOutput);

        // Source: just the base mod, no splits
        fs.Directory.CreateDirectory(SourceDir);
        fs.File.WriteAllText(SourcePatchPath, "base");

        // Destination: base + _2 + _3 (stale splits from previous run)
        fs.Directory.CreateDirectory(outputPath);
        fs.File.WriteAllText(Path.Combine(outputPath, "Synthesis.esp"), "old");
        fs.File.WriteAllText(Path.Combine(outputPath, "Synthesis_2.esp"), "old");
        fs.File.WriteAllText(Path.Combine(outputPath, "Synthesis_3.esp"), "old");

        sut.Move(SourcePatchPath, outputPath);

        // Base should have new content
        fs.File.ReadAllText(Path.Combine(outputPath, "Synthesis.esp")).ShouldBe("base");

        // All stale split files should be cleaned up
        fs.File.Exists(Path.Combine(outputPath, "Synthesis_2.esp")).ShouldBeFalse();
        fs.File.Exists(Path.Combine(outputPath, "Synthesis_3.esp")).ShouldBeFalse();
    }

    [Theory, SynthAutoData]
    public void CleansStaleStringsFilesAtDestination(
        IFileSystem fs,
        DirectoryPath workspaceOutput,
        DirectoryPath outputPath)
    {
        var sut = CreateSut(fs, workspaceOutput);

        // Source: base mod only, no strings (non-localized)
        fs.Directory.CreateDirectory(SourceDir);
        fs.File.WriteAllText(SourcePatchPath, "base");

        // Destination: has strings from previous localized run
        fs.Directory.CreateDirectory(outputPath);
        fs.Directory.CreateDirectory(Path.Combine(outputPath, "Strings"));
        fs.File.WriteAllText(Path.Combine(outputPath, "Synthesis.esp"), "old");
        fs.File.WriteAllText(Path.Combine(outputPath, "Strings", "Synthesis_English.STRINGS"), "old");
        fs.File.WriteAllText(Path.Combine(outputPath, "Strings", "Synthesis_English.DLSTRINGS"), "old");
        fs.File.WriteAllText(Path.Combine(outputPath, "Strings", "Synthesis_English.ILSTRINGS"), "old");

        sut.Move(SourcePatchPath, outputPath);

        // Base should have new content
        fs.File.ReadAllText(Path.Combine(outputPath, "Synthesis.esp")).ShouldBe("base");

        // All stale strings files should be cleaned up
        fs.File.Exists(Path.Combine(outputPath, "Strings", "Synthesis_English.STRINGS")).ShouldBeFalse();
        fs.File.Exists(Path.Combine(outputPath, "Strings", "Synthesis_English.DLSTRINGS")).ShouldBeFalse();
        fs.File.Exists(Path.Combine(outputPath, "Strings", "Synthesis_English.ILSTRINGS")).ShouldBeFalse();
    }

    [Theory, SynthAutoData]
    public void CopiesArchiveFiles(
        IFileSystem fs,
        DirectoryPath workspaceOutput,
        DirectoryPath outputPath)
    {
        var sut = CreateSut(fs, workspaceOutput);

        // Source: has BSA/BA2 files
        fs.Directory.CreateDirectory(SourceDir);
        fs.File.WriteAllText(SourcePatchPath, "base");
        fs.File.WriteAllText($"{SourceDir}/Synthesis.bsa", "archive");
        fs.File.WriteAllText($"{SourceDir}/Synthesis - Textures.ba2", "textures");

        sut.Move(SourcePatchPath, outputPath);

        // Base should be copied
        fs.File.ReadAllText(Path.Combine(outputPath, "Synthesis.esp")).ShouldBe("base");

        // Archives should be copied
        fs.File.ReadAllText(Path.Combine(outputPath, "Synthesis.bsa")).ShouldBe("archive");
        fs.File.ReadAllText(Path.Combine(outputPath, "Synthesis - Textures.ba2")).ShouldBe("textures");
    }

    [Theory, SynthAutoData]
    public void CleansStaleArchiveFilesAtDestination(
        IFileSystem fs,
        DirectoryPath workspaceOutput,
        DirectoryPath outputPath)
    {
        var sut = CreateSut(fs, workspaceOutput);

        // Source: base mod only, no archives
        fs.Directory.CreateDirectory(SourceDir);
        fs.File.WriteAllText(SourcePatchPath, "base");

        // Destination: has archives from previous run
        fs.Directory.CreateDirectory(outputPath);
        fs.File.WriteAllText(Path.Combine(outputPath, "Synthesis.esp"), "old");
        fs.File.WriteAllText(Path.Combine(outputPath, "Synthesis.bsa"), "old archive");
        fs.File.WriteAllText(Path.Combine(outputPath, "Synthesis - Textures.ba2"), "old textures");

        sut.Move(SourcePatchPath, outputPath);

        // Base should have new content
        fs.File.ReadAllText(Path.Combine(outputPath, "Synthesis.esp")).ShouldBe("base");

        // Stale archives should be cleaned up
        fs.File.Exists(Path.Combine(outputPath, "Synthesis.bsa")).ShouldBeFalse();
        fs.File.Exists(Path.Combine(outputPath, "Synthesis - Textures.ba2")).ShouldBeFalse();
    }
}
