using Autofac;
using Noggog;
using Noggog.Reactive;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Reporters;
using Synthesis.Bethesda.GUI.ViewModels;

namespace Synthesis.Bethesda.GUI.Services.Profile.ErrorClassification;

public class ErrorDisplayVmFactory
{
    private readonly ISchedulerProvider _schedulerProvider;
    private readonly IErrorClassifier _errorClassifier;
    private readonly IClassificationVmFactory _classificationVmFactory;
    private readonly ILifetimeScope _scope;

    public ErrorDisplayVmFactory(
        ISchedulerProvider schedulerProvider,
        IErrorClassifier errorClassifier,
        IClassificationVmFactory classificationVmFactory,
        ILifetimeScope scope)
    {
        _schedulerProvider = schedulerProvider;
        _errorClassifier = errorClassifier;
        _classificationVmFactory = classificationVmFactory;
        _scope = scope;
    }

    public ErrorDisplayVm Create(ISelected parent, IObservable<IConfigurationState> state)
    {
        return new ErrorDisplayVm(
            parent,
            state,
            _schedulerProvider,
            _errorClassifier,
            _classificationVmFactory,
            _scope);
    }
}