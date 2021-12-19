using System;
using System.Threading;
using System.Threading.Tasks;

namespace Synthesis.Bethesda.Execution.WorkEngine
{
    public interface IWorkDropoff
    {
        Task Enqueue(Action toDo, CancellationToken cancellationToken = default);
        Task Enqueue(Func<Task> toDo, CancellationToken cancellationToken = default);
        Task EnqueueAndWait(Action toDo, CancellationToken cancellationToken = default);
        Task<T> EnqueueAndWait<T>(Func<T> toDo, CancellationToken cancellationToken = default);
        Task EnqueueAndWait(Func<Task> toDo, CancellationToken cancellationToken = default);
        Task<T> EnqueueAndWait<T>(Func<Task<T>> toDo, CancellationToken cancellationToken = default);
    }
}