using System.Reactive.Linq;
using System.Windows.Input;
using Autofac;
using Noggog;
using Noggog.Reactive;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Reporters;
using Synthesis.Bethesda.GUI.Services.Profile.ErrorClassification;
using Synthesis.Bethesda.GUI.Services.Profile.Running;
using Synthesis.Bethesda.GUI.ViewModels.Profiles.Running;

namespace Synthesis.Bethesda.GUI.ViewModels;

public class ErrorDisplayVm : ViewModel
{
    [Reactive]
    public object DisplayedObject { get; set; }

    public ICommand GoToErrorCommand { get; }

    public ErrorVM ErrorVM { get; }

    // Tracks the current error object (either ErrorVM or a classified VM)
    private object? _currentErrorObject;

    private readonly ObservableAsPropertyHelper<IConfigurationState> _state;
    public IConfigurationState State => _state.Value;

    [Reactive]
    public string? ErrorTitle { get; private set; }

    public ErrorDisplayVm(
        ISelected parent,
        IObservable<IConfigurationState> state,
        ISchedulerProvider schedulerProvider,
        IErrorClassifier errorClassifier,
        IClassificationVmFactory classificationVmFactory,
        ILifetimeScope scope)
    {
        DisplayedObject = parent;

        _state = state
            .ToGuiProperty(this, nameof(State), ConfigurationState.Success, schedulerProvider.MainThread, deferSubscription: true);

        ErrorVM = new ErrorVM("Error", backAction: () =>
        {
            DisplayedObject = parent;
        });

        GoToErrorCommand = NoggogCommand.CreateFromObject(
            objectSource: state.Select(x => x.RunnableState),
            canExecute: x => x.Failed,
            execute: x =>
            {
                if (_currentErrorObject != null)
                {
                    DisplayedObject = _currentErrorObject;
                }
            },
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
                    // Try to classify the error if there's an exception
                    ErrorClassification? classification = null;
                    if (state.Exception != null)
                    {
                        classification = errorClassifier.Classify(state.Exception);
                    }

                    // If we have a classification, wrap it with a VM and display that instead
                    if (classification != null)
                    {
                        _currentErrorObject = classificationVmFactory.CreateVm(classification, scope);
                        ErrorTitle = classification.ErrorType;
                    }
                    else
                    {
                        // Fall back to plain text error display
                        ErrorVM.String = state.Reason;
                        _currentErrorObject = ErrorVM;
                        ErrorTitle = state.Reason?.Split(Environment.NewLine).FirstOrDefault();
                    }
                }
                else
                {
                    ErrorVM.String = null;
                    _currentErrorObject = null;
                    ErrorTitle = null;
                }
            })
            .DisposeWith(this);
    }
}