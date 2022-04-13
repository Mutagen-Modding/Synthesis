using System.IO.Abstractions;
using Mutagen.Bethesda.Environments.DI;
using Noggog;
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
    private readonly IDataDirectoryProvider _dataDirectoryProvider;
    public IFileSystem FileSystem { get; }

    public MoveFinalResults(
        IRunReporter reporter,
        IDataDirectoryProvider dataDirectoryProvider,
        IFileSystem fileSystem)
    {
        _reporter = reporter;
        _dataDirectoryProvider = dataDirectoryProvider;
        FileSystem = fileSystem;
    }
        
    public void Move(
        FilePath finalPatch,
        DirectoryPath outputPath)
    {
        if (!FileSystem.Directory.Exists(outputPath))
        {
            FileSystem.Directory.CreateDirectory(outputPath);
        }
            
        var finalPatchFolder = finalPatch.Directory!;
            
        _reporter.WriteOverall("Files to export:");
        foreach (var file in finalPatchFolder.Value.EnumerateFiles(true, fileSystem: FileSystem))
        {
            _reporter.WriteOverall($"   {file.GetRelativePathTo(finalPatchFolder.Value)}");
        }
            
        FileSystem.Directory.DeepCopy(finalPatchFolder.Value.Path, outputPath);
        _reporter.WriteOverall($"Exported patch to workspace: {outputPath}");
        FileSystem.Directory.DeepCopy(finalPatchFolder.Value.Path, _dataDirectoryProvider.Path, overwrite: true);
        _reporter.WriteOverall($"Exported patch to final destination: {_dataDirectoryProvider.Path}");
    }
}