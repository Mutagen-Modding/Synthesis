using System;

namespace Synthesis.Bethesda.Execution.GitRepository;

public interface IRepositoryCheckout : IDisposable
{
    IGitRepository Repository { get; }
}

public class RepositoryCheckout : IRepositoryCheckout
{
    private Lazy<IGitRepository> _Repository { get; }
    public IGitRepository Repository => _Repository.Value;
        
    private readonly IDisposable _Cleanup;

    public RepositoryCheckout(Lazy<IGitRepository> repo, IDisposable cleanup)
    {
        _Repository = repo;
        _Cleanup = cleanup;
    }

    public void Dispose()
    {
        if (_Repository.IsValueCreated)
        {
            _Repository.Value.Dispose();
        }
        _Cleanup.Dispose();
    }
}