using System;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using LibGit2Sharp;
using Noggog;
using Serilog;

namespace Synthesis.Bethesda.Execution.GitRepository
{
    public interface IProvideRepositoryCheckouts : IDisposable
    {
        IRepositoryCheckout Get(DirectoryPath path);
    }

    public class ProvideRepositoryCheckouts : IProvideRepositoryCheckouts
    {
        private readonly ILogger _Logger;
        private readonly TaskCompletionSource _shutdown = new();
        private int _numInFlight;
        private object _Lock = new();

        public bool IsShutdownRequested { get; private set; }
        public bool IsShutdown { get; private set; }

        public ProvideRepositoryCheckouts(ILogger logger)
        {
            _Logger = logger;
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
                    new Lazy<IGitRepository>(() => new GitRepository(new Repository(path))),
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
            _Logger.Information("Disposing repository jobs");
            lock (_Lock)
            {
                IsShutdownRequested = true;
                if (_numInFlight == 0)
                {
                    IsShutdown = true;
                }
                _Logger.Information("{NumInFlight} in flight repository jobs", _numInFlight == 0 ? "No" : _numInFlight);
            }

            if (IsShutdown)
            {
                _shutdown.TrySetResult();
            }

            _shutdown.Task.Wait();
            _Logger.Information("Finished disposing repository jobs");
        }
    }
}