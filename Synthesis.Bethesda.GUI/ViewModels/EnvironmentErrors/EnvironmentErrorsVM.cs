using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Noggog.WPF;
using ReactiveUI;

namespace Synthesis.Bethesda.GUI
{
    public interface IEnvironmentErrorsVM
    {
        bool InError { get; }
        IEnvironmentErrorVM? ActiveError { get; }
    }

    public class EnvironmentErrorsVM : ViewModel, IEnvironmentErrorsVM
    {
        private readonly ObservableAsPropertyHelper<bool> _InError;
        public bool InError => _InError.Value;

        private readonly ObservableAsPropertyHelper<IEnvironmentErrorVM?> _ActiveError;
        public IEnvironmentErrorVM? ActiveError => _ActiveError.Value;

        public EnvironmentErrorsVM(IEnumerable<IEnvironmentErrorVM> envErrors)
        {
            _ActiveError =
                Observable.CombineLatest(
                        envErrors.Select(env =>
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