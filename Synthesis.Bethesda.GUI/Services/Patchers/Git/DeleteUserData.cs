using System.IO.Abstractions;
using Noggog;
using Serilog;
using Synthesis.Bethesda.Execution.Patchers.Common;

namespace Synthesis.Bethesda.GUI.Services.Patchers.Git;

public class DeleteUserData
{
    private readonly IFileSystem _fileSystem;
    private readonly ILogger _logger;
    private readonly IPatcherExtraDataPathProvider _extraDataPathProvider;

    public DeleteUserData(
        IFileSystem fileSystem,
        ILogger logger,
        IPatcherExtraDataPathProvider extraDataPathProvider)
    {
        _fileSystem = fileSystem;
        _logger = logger;
        _extraDataPathProvider = extraDataPathProvider;
    }
        
    public void Delete()
    {
        try
        {
            _fileSystem.Directory.DeleteEntireFolder(_extraDataPathProvider.Path);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to delete user settings");
        }
    }
}