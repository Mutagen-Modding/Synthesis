using System.Reactive.Linq;
using Noggog.WPF;
using ReactiveUI;

namespace Synthesis.Bethesda.GUI
{
    public class EnvironmentErrorsVM : ViewModel
    {
        private readonly DotNetNotInstalledVM _DotNetInstalled;
        private readonly NugetConfigErrorVM _NugetConfig;
        
        private readonly ObservableAsPropertyHelper<bool> _InError;
        public bool InError => _InError.Value;

        private readonly ObservableAsPropertyHelper<IEnvironmentErrorVM?> _ActiveError;
        public IEnvironmentErrorVM? ActiveError => _ActiveError.Value;

        public EnvironmentErrorsVM(MainVM mvm)
        {
            _DotNetInstalled = new DotNetNotInstalledVM(mvm);
            _NugetConfig = new NugetConfigErrorVM();

            _ActiveError = Observable.CombineLatest(
                _DotNetInstalled.WhenAnyValue(x => x.InError)
                    .Select(x => x ? _DotNetInstalled : default(IEnvironmentErrorVM?)),
                _NugetConfig.WhenAnyValue(x => x.InError)
                    .Select(x => x ? _NugetConfig : default(IEnvironmentErrorVM?)),
                (dotNet, nuget) => dotNet ?? nuget)
                .ToGuiProperty(this, nameof(ActiveError), default(IEnvironmentErrorVM?));

            _InError = this.WhenAnyValue(x => x.ActiveError)
                .Select(x => x == null)
                .ToGuiProperty(this, nameof(InError));
        }
    }
}