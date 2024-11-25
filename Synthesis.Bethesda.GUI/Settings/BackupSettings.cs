using System.IO;
using System.IO.Abstractions;
using LibGit2Sharp;
using Noggog;
using Noggog.GitRepository;
using Serilog;
using Synthesis.Bethesda.Execution.Utility;

namespace Synthesis.Bethesda.GUI.Settings;

public class BackupSettings : IStartupTask
{
    private readonly ILogger _logger;
    private readonly IFileSystem _fileSystem;
    private readonly IInitRepository _initRepository;
    private readonly IProvideRepositoryCheckouts _repositoryCheckouts;

    private DirectoryPath RepoDirectory => Directory.GetCurrentDirectory();
    private FilePath GitIgnorePath => Path.Combine(RepoDirectory, ".gitignore");
    private FilePath PipelineSettings => Path.Combine(RepoDirectory, "PipelineSettings.json");
    private FilePath GuiSettings => Path.Combine(RepoDirectory, "GuiSettings.json");
    
    public BackupSettings(
        ILogger logger,
        IFileSystem fileSystem,
        IInitRepository initRepository,
        IProvideRepositoryCheckouts repositoryCheckouts)
    {
        _logger = logger;
        _fileSystem = fileSystem;
        _initRepository = initRepository;
        _repositoryCheckouts = repositoryCheckouts;
    }
    
    public void Start()
    {
        try
        {
            _initRepository.Init(RepoDirectory);
        }
        catch (LibGit2SharpException e)
        {
            _logger.Warning(e, "Could not initiate backup repository.");
            return;
        }
        try
        {
            CreateGitIgnore();
            using var repo = _repositoryCheckouts.Get(RepoDirectory);
            StageIfExists(GitIgnorePath, repo.Repository);
            StageIfExists(PipelineSettings, repo.Repository);
            StageIfExists(GuiSettings, repo.Repository);
            repo.Repository.Commit("Settings changed");
        }
        catch (EmptyCommitException)
        {
        }
        catch (Exception ex)
        {
            HandleException(ex);
        }
    }

    private void HandleException(Exception? ex)
    {
        switch (ex)
        {
            case LibGit2SharpException:
            case NullReferenceException:
                WipeBackup(ex);
                break;
            case AggregateException agg:
                HandleException(agg.InnerException);
                break;
            case null:
                return;
            default:
                throw ex;
        }
    }

    private void WipeBackup(Exception ex)
    {
        _logger.Warning(ex, "Wiping backup settings.");
        _fileSystem.Directory.DeleteEntireFolder(RepoDirectory);
    }

    private void CreateGitIgnore()
    {
        using var file = new StreamWriter(
            _fileSystem.File.Open(GitIgnorePath, FileMode.Create, FileAccess.Write));
        file.WriteLine("*");
        file.WriteLine("!.gitignore");
        file.WriteLine("!PipelineSettings.json");
        file.WriteLine("!GuiSettings.json");
    }

    private void StageIfExists(FilePath path, IGitRepository repo)
    {
        if (!_fileSystem.File.Exists(path)) return;
        _logger.Information("{Path} exists. Staging.", path);
        repo.Stage(path);
    }
}