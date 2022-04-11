using System.IO.Abstractions;
using LibGit2Sharp;
using Noggog;
using Noggog.IO;
using Serilog;

namespace Synthesis.Bethesda.Execution.GitRepository;

public interface ICheckIfKeeping
{
    bool ShouldKeep(
        DirectoryPath localDir,
        GetResponse<string> remoteUrl);
}

public class CheckIfKeeping : ICheckIfKeeping
{
    private readonly IFileSystem _fileSystem;
    private readonly ILogger _logger;
    public ICheckIfRepositoryDesirable IfRepositoryDesirable { get; }
    public IProvideRepositoryCheckouts RepoCheckouts { get; }

    public CheckIfKeeping(
        IFileSystem fileSystem,
        ILogger logger,
        ICheckIfRepositoryDesirable checkIfRepositoryDesirable,
        IProvideRepositoryCheckouts repoCheckouts)
    {
        _fileSystem = fileSystem;
        _logger = logger;
        IfRepositoryDesirable = checkIfRepositoryDesirable;
        RepoCheckouts = repoCheckouts;
    }
        
    public bool ShouldKeep(
        DirectoryPath localDir,
        GetResponse<string> remoteUrl)
    {
        if (!localDir.CheckExists(_fileSystem))
        {
            _logger.Information("No local repository exists at {LocalDirectory}.  No cleaning to do", localDir);
            return false;
        }
        if (remoteUrl.Failed)
        {
            _logger.Warning("No remote repository.  Deleting local at {LocalDirectory}", localDir);
            return false;
        }
        try
        {
            using var repoCheckout = RepoCheckouts.Get(localDir);

            if (!IfRepositoryDesirable.IsDesirable(repoCheckout.Repository)) return false;
                
            // If it's the same remote repo, don't delete
            if (repoCheckout.Repository.MainRemoteUrl?.Equals(remoteUrl.Value) ?? false)
            {
                _logger.Information("Remote repository target matched local folder's repo at {LocalDirectory}.  Keeping clone", localDir);
                return true;
            }
        }
        catch (RepositoryNotFoundException)
        {
            _logger.Error("Repository corrupted.  Deleting local at {LocalDirectory}", localDir);
            return false;
        }

        _logger.Information("Remote address targeted a different repository.  Deleting local at {LocalDirectory}", localDir);
        return false;
    }
}