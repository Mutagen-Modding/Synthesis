using System.Reactive.Disposables;
using Noggog;
using Serilog;

namespace Synthesis.Bethesda.Execution.GitRepository;

public interface IProvideRepositoryCheckouts : IDisposable
{
    IRepositoryCheckout Get(DirectoryPath path);
}

public class ProvideRepositoryCheckouts : IProvideRepositoryCheckouts
{
    private readonly ILogger _logger;
    public IGitRepositoryFactory RepositoryFactory { get; }
        
    private readonly TaskCompletionSource _shutdown = new();
    private int _numInFlight;
    private object _Lock = new();

    public bool IsShutdownRequested { get; private set; }
    public bool IsShutdown { get; private set; }

    public ProvideRepositoryCheckouts(
        ILogger logger,
        IGitRepositoryFactory repositoryFactory)
    {
        _logger = logger;
        RepositoryFactory = repositoryFactory;
    }

    public IRepositoryCheckout Get(DirectoryPath path)
    {
        lock (_Lock)
        {
            if (IsShutdown)
            {
                throw new InvalidOperationException("Tried to get a repository from a shut down manager");
            }

            _numInFlight++;
            return new RepositoryCheckout(
                new Lazy<IGitRepository>(() => RepositoryFactory.Get(path)),
                Disposable.Create(Cleanup));
        }
    }

    private void Cleanup()
    {
        lock (_Lock)
        {
            _numInFlight--;
            if (IsShutdownRequested
                && _numInFlight == 0)
            {
                IsShutdown = true;
            }
        }

        if (IsShutdown)
        {
            _shutdown.TrySetResult();
        }
    }

    public void Dispose()
    {
        _logger.Information("Disposing repository jobs");
        lock (_Lock)
        {
            IsShutdownRequested = true;
            if (_numInFlight == 0)
            {
                IsShutdown = true;
            }
            _logger.Information("{NumInFlight} in flight repository jobs", _numInFlight == 0 ? "No" : _numInFlight);
        }

        if (IsShutdown)
        {
            _shutdown.TrySetResult();
        }

        _shutdown.Task.Wait();
        _logger.Information("Finished disposing repository jobs");
    }
}