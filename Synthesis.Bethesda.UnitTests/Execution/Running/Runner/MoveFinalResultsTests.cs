using System.IO.Abstractions;
using Shouldly;
using Noggog;
using Synthesis.Bethesda.Execution.Running.Runner;
using Synthesis.Bethesda.UnitTests.AutoData;

namespace Synthesis.Bethesda.UnitTests.Execution.Running.Runner;

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
        fs.Directory.Exists(missingOutputPath).ShouldBeTrue();
    }
        
    [Theory, SynthAutoData]
    public void MovesFilesToWorkspaceFinalDestination(
        IFileSystem fs,
        DirectoryPath missingOutputPath,
        MoveFinalResults sut)
    {
        PrepFileSystem(fs);
        sut.Move(SourcePatchPath, missingOutputPath);
        fs.File.Exists(Path.Combine(missingOutputPath, "Synthesis.esp")).ShouldBeTrue();
        fs.File.Exists(Path.Combine(missingOutputPath, "Strings", "Synthesis_English.STRINGS")).ShouldBeTrue();
    }
        
    [Theory, SynthAutoData]
    public void MovesFilesToWorkspaceDir(
        IFileSystem fs,
        DirectoryPath missingOutputPath,
        MoveFinalResults sut)
    {
        PrepFileSystem(fs);
        sut.Move(SourcePatchPath, missingOutputPath);
        fs.File.Exists(Path.Combine(sut.ProfileDirectories.OutputDirectory.Path, "Synthesis.esp")).ShouldBeTrue();
        fs.File.Exists(Path.Combine(sut.ProfileDirectories.OutputDirectory.Path, "Strings", "Synthesis_English.STRINGS")).ShouldBeTrue();
    }
        
    [Theory, SynthAutoData]
    public void OverwritesFilesInFinalDestination(
        IFileSystem fs,
        DirectoryPath existingOutputDir,
        MoveFinalResults sut)
    {
        PrepFileSystem(fs);
        fs.File.WriteAllText(Path.Combine(existingOutputDir.Path, "Synthesis.esp"), "Hello");
        fs.Directory.CreateDirectory(Path.Combine(existingOutputDir.Path, "Strings"));
        fs.File.WriteAllText(Path.Combine(existingOutputDir.Path, "Strings", "Synthesis_English.STRINGS"), "World");
        sut.Move(SourcePatchPath, existingOutputDir);
        fs.File.ReadAllText(Path.Combine(existingOutputDir.Path, "Synthesis.esp")).ShouldBe(string.Empty);
        fs.File.ReadAllText(Path.Combine(existingOutputDir.Path, "Strings", "Synthesis_English.STRINGS")).ShouldBe(string.Empty);
    }
}