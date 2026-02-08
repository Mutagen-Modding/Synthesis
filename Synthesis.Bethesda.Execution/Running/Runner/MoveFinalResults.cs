using System.IO.Abstractions;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.IO.DI;
using Noggog;
using Synthesis.Bethesda.Execution.Profile.Services;
using Synthesis.Bethesda.Execution.Reporters;

namespace Synthesis.Bethesda.Execution.Running.Runner;

public interface IMoveFinalResults
{
    IReadOnlyList<ModKey> Move(
        FilePath finalPatch,
        DirectoryPath outputPath);
}

public class MoveFinalResults : IMoveFinalResults
{
    private readonly IRunReporter _reporter;
    private readonly IModFilesMover _modFilesMover;
    private readonly IAssociatedFilesLocator _associatedFilesLocator;
    public IProfileDirectories ProfileDirectories { get; }
    public IFileSystem FileSystem { get; }

    public MoveFinalResults(
        IRunReporter reporter,
        IProfileDirectories profileProfileDirectories,
        IFileSystem fileSystem,
        IModFilesMover modFilesMover,
        IAssociatedFilesLocator associatedFilesLocator)
    {
        _reporter = reporter;
        _modFilesMover = modFilesMover;
        _associatedFilesLocator = associatedFilesLocator;
        ProfileDirectories = profileProfileDirectories;
        FileSystem = fileSystem;
    }

    public IReadOnlyList<ModKey> Move(
        FilePath finalPatch,
        DirectoryPath outputPath)
    {
        var workspaceOutput = ProfileDirectories.OutputDirectory;
        if (!FileSystem.Directory.Exists(workspaceOutput))
        {
            FileSystem.Directory.CreateDirectory(workspaceOutput);
        }
        if (!FileSystem.Directory.Exists(outputPath))
        {
            FileSystem.Directory.CreateDirectory(outputPath);
        }

        var associatedFiles = _associatedFilesLocator.GetAssociatedFiles(finalPatch).ToList();

        _reporter.WriteOverall("Files to export:");
        foreach (var file in associatedFiles)
        {
            _reporter.WriteOverall($"   {file.GetRelativePathTo(finalPatch.Directory!.Value)}");
        }

        _modFilesMover.CopyModTo(finalPatch, workspaceOutput, overwrite: true);
        _reporter.WriteOverall($"Exported patch to workspace: {workspaceOutput}");
        _modFilesMover.CopyModTo(finalPatch, outputPath, overwrite: true);
        _reporter.WriteOverall($"Exported patch to final destination: {outputPath.Path}");

        // Return the ModKeys for the plugin files that were moved
        return associatedFiles
            .Select(f => (FileName?)f.Name)
            .SelectWhere<FileName?, ModKey>(ModKey.TryFromFileName)
            .ToList();
    }
}