using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Noggog;

namespace Synthesis.Bethesda.Execution.GitRespository
{
    public interface IProvideRepositoryCheckouts : IAsyncDisposable
    {
        RepositoryCheckout Get(DirectoryPath path);
    }

    public class ProvideRepositoryCheckouts : IProvideRepositoryCheckouts
    {
        private readonly TaskCompletionSource _shutdown = new();
        private int _numInFlight;
        private object _Lock = new();

        public bool IsShutdownRequested { get; private set; }
        public bool IsShutdown { get; private set; }

        public ProvideRepositoryCheckouts()
        {
        }

        public RepositoryCheckout Get(DirectoryPath path)
        {
            lock (_Lock)
            {
                if (IsShutdown)
                {
                    throw new InvalidOperationException("Tried to get a repository from a shut down manager");
                }

                _numInFlight++;
                return new RepositoryCheckout(
                    path,
                    Disposable.Create(() => Cleanup()));
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

        public async ValueTask DisposeAsync()
        {
            lock (_Lock)
            {
                IsShutdownRequested = true;
                if (_numInFlight == 0)
                {
                    IsShutdown = true;
                }
            }

            if (IsShutdown)
            {
                _shutdown.TrySetResult();
            }

            await _shutdown.Task.ConfigureAwait(false);
        }
    }
}