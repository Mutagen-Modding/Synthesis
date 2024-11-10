using System.IO.Abstractions;
using Noggog;
using Serilog;
using Synthesis.Bethesda.Execution.DotNet.Dto;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Patchers.Git.Services;

namespace Synthesis.Bethesda.Execution.Patchers.Running.Git;

public class BuildDirectoryCleaner
{
    private readonly IFileSystem _fileSystem;
    private readonly ILogger _logger;

    public BuildDirectoryCleaner(
        IFileSystem fileSystem,
        ILogger logger)
    {
        _fileSystem = fileSystem;
        _logger = logger;
    }
    
    public void Clean(
        RunnerRepoInfo info,
        DotNetVersion dotNetVersion,
        GitCompilationMeta? meta)
    {
        if (dotNetVersion.Version == meta?.NetSdkVersion) return;
        var buildFolder = Path.Combine(info.Project.ProjPath.Directory!, "bin");
        var objFolder = Path.Combine(info.Project.ProjPath.Directory!, "obj");
        _logger.Information("Deleting build folder", buildFolder);
        _fileSystem.Directory.DeleteEntireFolder(buildFolder);
        _logger.Information("Deleting obj folder", objFolder);
        _fileSystem.Directory.DeleteEntireFolder(objFolder);
    }
}