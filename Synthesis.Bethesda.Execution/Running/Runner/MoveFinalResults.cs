using System.IO.Abstractions;
using Mutagen.Bethesda.Environments.DI;
using Noggog;
using Synthesis.Bethesda.Execution.Profile;
using Synthesis.Bethesda.Execution.Reporters;

namespace Synthesis.Bethesda.Execution.Running.Runner;

public interface IMoveFinalResults
{
    void Move(
        FilePath finalPatch,
        DirectoryPath outputPath);
}

public class MoveFinalResults : IMoveFinalResults
{
    private readonly IRunReporter _reporter;
    private readonly IProfileDirectories _profileDirectories;
    public IFileSystem FileSystem { get; }

    public MoveFinalResults(
        IRunReporter reporter,
        IProfileDirectories profileDirectories,
        IFileSystem fileSystem)
    {
        _reporter = reporter;
        _profileDirectories = profileDirectories;
        FileSystem = fileSystem;
    }
        
    public void Move(
        FilePath finalPatch,
        DirectoryPath outputPath)
    {
        var workspaceOutput = _profileDirectories.OutputDirectory;
        if (!FileSystem.Directory.Exists(workspaceOutput))
        {
            FileSystem.Directory.CreateDirectory(workspaceOutput);
        }
            
        var finalPatchFolder = finalPatch.Directory!;
            
        _reporter.WriteOverall("Files to export:");
        foreach (var file in finalPatchFolder.Value.EnumerateFiles(true, fileSystem: FileSystem))
        {
            _reporter.WriteOverall($"   {file.GetRelativePathTo(finalPatchFolder.Value)}");
        }
            
        FileSystem.Directory.DeepCopy(finalPatchFolder.Value.Path, workspaceOutput);
        _reporter.WriteOverall($"Exported patch to workspace: {workspaceOutput}");
        FileSystem.Directory.DeepCopy(finalPatchFolder.Value.Path, outputPath.Path, overwrite: true);
        _reporter.WriteOverall($"Exported patch to final destination: {outputPath.Path}");
    }
}