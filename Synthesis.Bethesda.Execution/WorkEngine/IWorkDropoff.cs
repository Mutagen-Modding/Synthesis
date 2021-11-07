using System;
using System.Threading;
using System.Threading.Tasks;

namespace Synthesis.Bethesda.Execution.WorkEngine
{
    public interface IWorkDropoff
    {
        ValueTask Enqueue(Action toDo, CancellationToken cancellationToken = default);
        ValueTask Enqueue(Func<Task> toDo, CancellationToken cancellationToken = default);
        ValueTask EnqueueAndWait(Action toDo, CancellationToken cancellationToken = default);
        ValueTask<T> EnqueueAndWait<T>(Func<T> toDo, CancellationToken cancellationToken = default);
        ValueTask EnqueueAndWait(Func<Task> toDo, CancellationToken cancellationToken = default);
        ValueTask<T> EnqueueAndWait<T>(Func<Task<T>> toDo, CancellationToken cancellationToken = default);
    }
}