using System;
using System.Threading;
using System.Threading.Tasks;

namespace Synthesis.Bethesda.Execution.WorkEngine
{
    public class InlineWorkDropoff : IWorkDropoff
    {
        public async Task Enqueue(Action toDo, CancellationToken cancellationToken = default)
        {
            toDo();
        }

        public async Task Enqueue(Func<Task> toDo, CancellationToken cancellationToken = default)
        {
            await toDo();
        }

        public async Task EnqueueAndWait(Action toDo, CancellationToken cancellationToken = default)
        {
            toDo();
        }

        public async Task<T> EnqueueAndWait<T>(Func<T> toDo, CancellationToken cancellationToken = default)
        {
            return toDo();
        }

        public async Task EnqueueAndWait(Func<Task> toDo, CancellationToken cancellationToken = default)
        {
            await toDo();
        }

        public async Task<T> EnqueueAndWait<T>(Func<Task<T>> toDo, CancellationToken cancellationToken = default)
        {
            return await toDo();
        }
    }
}