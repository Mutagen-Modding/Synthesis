using System.IO.Abstractions;
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
    public IProfileDirectories ProfileDirectories { get; }
    public IFileSystem FileSystem { get; }

    public MoveFinalResults(
        IRunReporter reporter,
        IProfileDirectories profileProfileDirectories,
        IFileSystem fileSystem)
    {
        _reporter = reporter;
        ProfileDirectories = profileProfileDirectories;
        FileSystem = fileSystem;
    }
        
    public void Move(
        FilePath finalPatch,
        DirectoryPath outputPath)
    {
        var workspaceOutput = ProfileDirectories.OutputDirectory;
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