using System;
using LibGit2Sharp;
using Noggog;

namespace Synthesis.Bethesda.Execution.GitRespository
{
    public class RepositoryCheckout : IDisposable
    {
        private Lazy<Repository> _Repository { get; }
        public Repository Repository => _Repository.Value;
        
        private readonly IDisposable _Cleanup;

        public RepositoryCheckout(Lazy<Repository> repo, IDisposable cleanup)
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
}