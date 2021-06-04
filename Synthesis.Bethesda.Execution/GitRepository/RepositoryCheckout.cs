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

        public RepositoryCheckout(DirectoryPath path, IDisposable cleanup)
        {
            _Repository = new Lazy<Repository>(() => new Repository(path));
            _Cleanup = cleanup;
        }

        public void Dispose()
        {
            Repository.Dispose();
            _Cleanup.Dispose();
        }
    }
}