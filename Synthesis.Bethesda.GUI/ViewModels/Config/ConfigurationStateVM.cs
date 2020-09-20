using Noggog;
using Noggog.WPF;
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Text;

namespace Synthesis.Bethesda.GUI
{
    public class ConfigurationStateVM : ConfigurationStateVM<Unit>
    {
        public static readonly ConfigurationStateVM Success = new ConfigurationStateVM();

        public ConfigurationStateVM() 
            : base(Unit.Default)
        {
        }

        public ConfigurationStateVM(ErrorResponse err)
            : base(Unit.Default, err)
        {
        }
    }

    public class ConfigurationStateVM<T> : ViewModel
    {
        public bool IsHaltingError { get; set; }
        public ErrorResponse RunnableState { get; set; } = ErrorResponse.Success;
        public T Item { get; }

        public ConfigurationStateVM(T item)
        {
            Item = item;
        }

        public ConfigurationStateVM(T item, ErrorResponse err)
            : this(item)
        {
            IsHaltingError = err.Failed;
            RunnableState = err;
        }

        public ConfigurationStateVM(GetResponse<T> resp)
            : this(resp.Value)
        {
            IsHaltingError = resp.Failed;
            RunnableState = resp;
        }

        public ConfigurationStateVM ToUnit()
        {
            return new ConfigurationStateVM()
            {
                IsHaltingError = this.IsHaltingError,
                RunnableState = this.RunnableState,
            };
        }

        public ConfigurationStateVM<R> BubbleError<R>()
        {
            return new ConfigurationStateVM<R>(default!)
            {
                IsHaltingError = this.IsHaltingError,
                RunnableState = this.RunnableState
            };
        }

        public GetResponse<T> ToGetResponse()
        {
            return GetResponse<T>.Create(RunnableState.Succeeded, Item, RunnableState.Reason);
        }
    }
}
