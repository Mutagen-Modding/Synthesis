using Noggog;
using System.Diagnostics.CodeAnalysis;
using System.Reactive;

namespace Synthesis.Bethesda.Execution.Patchers.Git;

public interface IConfigurationState
{
    bool IsHaltingError { get; }
    ErrorResponse RunnableState { get; }
}
    
public class ConfigurationState : ConfigurationState<Unit>
{
    public static readonly ConfigurationState Success = new();

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

[ExcludeFromCodeCoverage]
public class ConfigurationState<T> : IConfigurationState
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

    public ConfigurationState BubbleError()
    {
        return new ConfigurationState(default!)
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

    public override string ToString()
    {
        return $"{RunnableState}, Halting: {IsHaltingError}";
    }
}