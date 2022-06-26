using Noggog.WPF;
using ReactiveUI.Fody.Helpers;

namespace Synthesis.Bethesda.GUI.Settings;

public interface ILockToCurrentVersioning
{
    bool Lock { get; set; }
}

public class LockToCurrentVersioning : ViewModel, ILockToCurrentVersioning
{
    [Reactive]
    public bool Lock { get; set; }
}