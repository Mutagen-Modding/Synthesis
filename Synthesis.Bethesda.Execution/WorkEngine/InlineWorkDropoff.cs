using System;
using System.Threading;
using System.Threading.Tasks;

namespace Synthesis.Bethesda.Execution.WorkEngine
{
    public class InlineWorkDropoff : IWorkDropoff
    {
        public async ValueTask Enqueue(Action toDo, CancellationToken cancellationToken = default)
        {
            toDo();
        }

        public async ValueTask Enqueue(Func<Task> toDo, CancellationToken cancellationToken = default)
        {
            await toDo();
        }

        public async ValueTask EnqueueAndWait(Action toDo, CancellationToken cancellationToken = default)
        {
            toDo();
        }

        public async ValueTask<T> EnqueueAndWait<T>(Func<T> toDo, CancellationToken cancellationToken = default)
        {
            return toDo();
        }

        public async ValueTask EnqueueAndWait(Func<Task> toDo, CancellationToken cancellationToken = default)
        {
            await toDo();
        }

        public async ValueTask<T> EnqueueAndWait<T>(Func<Task<T>> toDo, CancellationToken cancellationToken = default)
        {
            return await toDo();
        }
    }
}