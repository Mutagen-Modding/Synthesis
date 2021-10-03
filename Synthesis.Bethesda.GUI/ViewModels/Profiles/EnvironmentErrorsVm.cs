using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Noggog.WPF;
using ReactiveUI;
using Serilog;
using Synthesis.Bethesda.GUI.ViewModels.EnvironmentErrors;

namespace Synthesis.Bethesda.GUI.ViewModels.Profiles
{
    public interface IEnvironmentErrorsVm
    {
        bool InError { get; }
        IEnvironmentErrorVm? ActiveError { get; }
    }

    public class EnvironmentErrorsVm : ViewModel, IEnvironmentErrorsVm
    {
        private readonly ObservableAsPropertyHelper<bool> _InError;
        public bool InError => _InError.Value;

        private readonly ObservableAsPropertyHelper<IEnvironmentErrorVm?> _ActiveError;
        public IEnvironmentErrorVm? ActiveError => _ActiveError.Value;

        public EnvironmentErrorsVm(
            ILogger logger,
            IEnumerable<IEnvironmentErrorVm> envErrors)
        {
            var envErrorsArr = envErrors.ToArray();

            if (envErrorsArr.Length == 0)
            {
                logger.Warning("No environment errors registered");
            }
            
            _ActiveError =
                Observable.CombineLatest(
                        envErrorsArr.Select(env =>
                        {
                            return env
                                .WhenAnyValue(x => x.InError)
                                .Select(inErr => inErr ? env : default);
                        }),
                        (errs) => errs.FirstOrDefault(e => e != null))
                    .ToGuiProperty(this, nameof(ActiveError), default);

            _InError = this.WhenAnyValue(x => x.ActiveError)
                .Select(x => x == null)
                .ToGuiProperty(this, nameof(InError));
        }
    }
}