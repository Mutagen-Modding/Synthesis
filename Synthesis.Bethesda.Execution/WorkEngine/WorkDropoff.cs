using System.Threading.Channels;

namespace Synthesis.Bethesda.Execution.WorkEngine;

public class WorkDropoff : IWorkDropoff, IWorkQueue
{
    private readonly Channel<IToDo> _channel = Channel.CreateUnbounded<IToDo>();
    public ChannelReader<IToDo> Reader => _channel.Reader;

    private async Task ProcessExistingQueue()
    {
        if (WorkConsumer.AsyncLocalCurrentQueue.Value == null) return;
        while (WorkConsumer.AsyncLocalCurrentQueue.Value.Reader.TryRead(out var toDo))
        {
            if (toDo.IsAsync)
            {
                await toDo.DoAsync();
            }
            else
            {
                toDo.Do();
            }
        }
    }

    public async Task Enqueue(Action toDo, CancellationToken cancellationToken = default)
    {
        await _channel.Writer.WriteAsync(new ToDo(toDo, null), cancellationToken).ConfigureAwait(false);
    }

    public async Task EnqueueAndWait(Action toDo, CancellationToken cancellationToken = default)
    {
        var tcs = new TaskCompletionSource();
        await _channel.Writer.WriteAsync(new ToDo(toDo, tcs), cancellationToken).ConfigureAwait(false);
        await ProcessExistingQueue().ConfigureAwait(false);
        await tcs.Task.ConfigureAwait(false);
    }

    public async Task<T> EnqueueAndWait<T>(Func<T> toDo, CancellationToken cancellationToken = default)
    {
        var tcs = new TaskCompletionSource<T>();
        await _channel.Writer.WriteAsync(new ToDo<T>(toDo, tcs), cancellationToken).ConfigureAwait(false);
        await ProcessExistingQueue().ConfigureAwait(false);
        return await tcs.Task.ConfigureAwait(false);
    }

    public async Task Enqueue(Func<Task> toDo, CancellationToken cancellationToken = default)
    {
        await _channel.Writer.WriteAsync(new ToDo(toDo, null), cancellationToken).ConfigureAwait(false);
    }
        
    public async Task EnqueueAndWait(Func<Task> toDo, CancellationToken cancellationToken = default)
    {
        var tcs = new TaskCompletionSource();
        await _channel.Writer.WriteAsync(new ToDo(toDo, tcs), cancellationToken).ConfigureAwait(false);
        await ProcessExistingQueue().ConfigureAwait(false);
        await tcs.Task.ConfigureAwait(false);
    }

    public async Task<T> EnqueueAndWait<T>(Func<Task<T>> toDo, CancellationToken cancellationToken = default)
    {
        var tcs = new TaskCompletionSource<T>();
        await _channel.Writer.WriteAsync(new ToDo<T>(toDo, tcs), cancellationToken).ConfigureAwait(false);
        await ProcessExistingQueue().ConfigureAwait(false);
        return await tcs.Task.ConfigureAwait(false);
    }
}