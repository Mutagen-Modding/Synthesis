using System.Reactive.Linq;
using Noggog;
using Noggog.WPF;
using ReactiveUI.Fody.Helpers;

namespace Synthesis.Bethesda.GUI.ViewModels.Top;

public record ActiveViewModel(ViewModel ViewModel, Func<Task>? OnDeactivation)
{
    public static implicit operator ActiveViewModel?(ViewModel? vm)
    {
        if (vm == null) return null;
        return new ActiveViewModel(vm, async () => { });
    }
}
    
public interface IActivePanelControllerVm
{
    ActiveViewModel? ActivePanel { get; set; }
}

public class ActivePanelControllerVm : ViewModel, IActivePanelControllerVm
{
    [Reactive]
    public ActiveViewModel? ActivePanel { get; set; }

    public ActivePanelControllerVm()
    {
        this.WhenAnyFallback(x => x.ActivePanel)
            .Pairwise()
            .Select(x => x.Previous?.OnDeactivation)
            .NotNull()
            .Subscribe(x => x());
    }
}