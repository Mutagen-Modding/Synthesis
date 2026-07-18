using Noggog.UI;
using Noggog.WPF;
using ReactiveUI;

namespace Synthesis.Bethesda.GUI.Services.Startup;

public interface IStartupTracker
{
    bool Initialized { get; set; }
}

public class StartupTracker : ViewModel, IStartupTracker
{
    private bool _initialized;
    public bool Initialized
    {
        get => _initialized;
        set => this.RaiseAndSetIfChanged(ref _initialized, value);
    }
}