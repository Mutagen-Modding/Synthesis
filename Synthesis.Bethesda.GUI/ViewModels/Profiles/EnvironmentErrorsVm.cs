using System.Reactive.Linq;
using Noggog.WPF;
using ReactiveUI;
using Serilog;
using Synthesis.Bethesda.GUI.ViewModels.EnvironmentErrors;

namespace Synthesis.Bethesda.GUI.ViewModels.Profiles;

public interface IEnvironmentErrorsVm
{
    bool InError { get; }
    IEnvironmentErrorVm? ActiveError { get; }
}

public class EnvironmentErrorsVm : ViewModel, IEnvironmentErrorsVm
{
    private readonly ObservableAsPropertyHelper<bool> _inError;
    public bool InError => _inError.Value;

    private readonly ObservableAsPropertyHelper<IEnvironmentErrorVm?> _activeError;
    public IEnvironmentErrorVm? ActiveError => _activeError.Value;

    public EnvironmentErrorsVm(
        ILogger logger,
        IEnumerable<IEnvironmentErrorVm> envErrors)
    {
        var envErrorsArr = envErrors.ToArray();

        if (envErrorsArr.Length == 0)
        {
            logger.Warning("No environment errors registered");
        }
            
        _activeError =
            Observable.CombineLatest(
                    envErrorsArr.Select(env =>
                    {
                        return env
                            .WhenAnyValue(x => x.InError)
                            .Select(inErr => inErr ? env : default);
                    }),
                    (errs) => errs.FirstOrDefault(e => e != null))
                .ToGuiProperty(this, nameof(ActiveError), default, deferSubscription: true);

        _inError = this.WhenAnyValue(x => x.ActiveError)
            .Select(x => x == null)
            .ToGuiProperty(this, nameof(InError), deferSubscription: true);
    }
}