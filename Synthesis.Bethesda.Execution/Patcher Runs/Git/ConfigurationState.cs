using Noggog;
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Text;

namespace Synthesis.Bethesda.Execution
{
    public class ConfigurationState : ConfigurationState<Unit>
    {
        public static readonly ConfigurationState Success = new ConfigurationState();

        public ConfigurationState() 
            : base(Unit.Default)
        {
        }

        public ConfigurationState(ErrorResponse err)
            : base(Unit.Default, err)
        {
        }

        public static implicit operator ConfigurationState(ErrorResponse err)
        {
            return new ConfigurationState(err);
        }
    }

    public class ConfigurationState<T>
    {
        public bool IsHaltingError { get; set; }
        public ErrorResponse RunnableState { get; set; } = ErrorResponse.Success;
        public T Item { get; }

        public ConfigurationState(T item)
        {
            Item = item;
        }

        public ConfigurationState(T item, ErrorResponse err)
            : this(item)
        {
            IsHaltingError = err.Failed;
            RunnableState = err;
        }

        public ConfigurationState(GetResponse<T> resp)
            : this(resp.Value)
        {
            IsHaltingError = resp.Failed;
            RunnableState = resp;
        }

        public ConfigurationState ToUnit()
        {
            return new ConfigurationState()
            {
                IsHaltingError = this.IsHaltingError,
                RunnableState = this.RunnableState,
            };
        }

        public ConfigurationState<R> BubbleError<R>()
        {
            return new ConfigurationState<R>(default!)
            {
                IsHaltingError = this.IsHaltingError,
                RunnableState = this.RunnableState
            };
        }

        public GetResponse<T> ToGetResponse()
        {
            return GetResponse<T>.Create(RunnableState.Succeeded, Item, RunnableState.Reason);
        }

        public static implicit operator ConfigurationState<T>(GetResponse<T> err)
        {
            return new ConfigurationState<T>(err);
        }
    }
}
