using System;
using System.Threading.Tasks;

namespace Synthesis.Bethesda.Execution.WorkEngine
{
    public interface IToDo
    {
        bool IsAsync { get; }
        public void Do();
        public Task DoAsync();
    }
    
    public class ToDo : IToDo
    {
        public readonly Action? Action;
        public readonly Func<Task>? Task;
        public readonly TaskCompletionSource? Complete;

        public bool IsAsync => Task != null;

        public ToDo(Action? action, TaskCompletionSource? tcs)
        {
            Action = action;
            Complete = tcs;
            Task = null;
        }

        public ToDo(Func<Task>? action, TaskCompletionSource? tcs)
        {
            Action = null;
            Complete = tcs;
            Task = action;
        }

        public void Do()
        {
            try
            {
                Action!();
                Complete?.SetResult();
            }
            catch (Exception e)
            {
                Complete?.SetException(e);
            }
        }

        public async Task DoAsync()
        {
            try
            {
                await Task!().ConfigureAwait(false);
                Complete?.SetResult();
            }
            catch (Exception e)
            {
                Complete?.SetException(e);
            }
        }
    }
    
    public class ToDo<T> : IToDo
    {
        public readonly Func<T>? Action;
        public readonly Func<Task<T>>? Task;
        public readonly TaskCompletionSource<T>? Complete;

        public bool IsAsync => Task != null;

        public ToDo(Func<T>? action, TaskCompletionSource<T>? tcs)
        {
            Action = action;
            Complete = tcs;
            Task = null;
        }

        public ToDo(Func<Task<T>>? action, TaskCompletionSource<T>? tcs)
        {
            Action = null;
            Complete = tcs;
            Task = action;
        }

        public void Do()
        {
            try
            {
                var ret = Action!();
                Complete?.SetResult(ret);
            }
            catch (Exception e)
            {
                Complete?.SetException(e);
            }
        }

        public async Task DoAsync()
        {
            try
            {
                var ret = await Task!().ConfigureAwait(false);
                Complete?.SetResult(ret);
            }
            catch (Exception e)
            {
                Complete?.SetException(e);
            }
        }
    }
}