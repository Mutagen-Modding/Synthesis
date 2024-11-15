using System.Reactive.Linq;
using System.Windows.Input;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Synthesis.Bethesda.Execution.Patchers.Git.Services;
using Synthesis.Bethesda.GUI.ViewModels.Profiles.Running;

namespace Synthesis.Bethesda.GUI.ViewModels;

public class ErrorDisplayVm : ViewModel
{
    [Reactive]
    public object DisplayedObject { get; set; }

    public ICommand GoToErrorCommand { get; }

    public ErrorVM ErrorVM { get; }

    private readonly ObservableAsPropertyHelper<IConfigurationState> _state;
    public IConfigurationState State => _state.Value;

    public ErrorDisplayVm(
        ISelected parent,
        IObservable<IConfigurationState> state)
    {
        DisplayedObject = parent;

        _state = state
            .ToGuiProperty(this, nameof(State), ConfigurationState.Success, deferSubscription: true);
            
        ErrorVM = new ErrorVM("Error", backAction: () =>
        {
            DisplayedObject = parent;
        });
            
        GoToErrorCommand = NoggogCommand.CreateFromObject(
            objectSource: state.Select(x => x.RunnableState),
            canExecute: x => x.Failed,
            execute: x => DisplayedObject = ErrorVM,
            disposable: this);

        parent.WhenAnyValue(x => x.IsSelected)
            .DistinctUntilChanged()
            .Where(x => x)
            .Subscribe(_ =>
            {
                DisplayedObject = parent;
            })
            .DisposeWith(this);

        state.Select(x => x.RunnableState)
            .Subscribe(state =>
            {
                if (state.Failed)
                {
                    ErrorVM.String = state.Reason;
                }
                else
                {
                    ErrorVM.String = null;
                }
            })
            .DisposeWith(this);
    }
}