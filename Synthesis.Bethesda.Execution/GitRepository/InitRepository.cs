using LibGit2Sharp;
using Noggog;
using Serilog;

namespace Synthesis.Bethesda.Execution.GitRepository;

public interface IInitRepository
{
    void Init(DirectoryPath folder);
}

public class InitRepository : IInitRepository
{
    private readonly ILogger _logger;

    public InitRepository(ILogger logger)
    {
        _logger = logger;
    }
        
    public void Init(DirectoryPath folder)
    {
        if (!Repository.IsValid(folder))
        {
            _logger.Information("Initializing repository at {Folder}", folder);
            Repository.Init(folder);
        }
    }
}