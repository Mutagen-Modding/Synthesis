using System.Reactive.Linq;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Synthesis.Bethesda.Execution.Patchers.Solution;
using Synthesis.Bethesda.GUI.Services.Patchers.Solution;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.TopLevel;

namespace Synthesis.Bethesda.GUI.ViewModels.Patchers.Solution;

public class SolutionPatcherNameVm : ViewModel, IPatcherNameVm
{
    private readonly ObservableAsPropertyHelper<string> _name;
    public string Name => _name.Value;

    [Reactive] public string Nickname { get; set; } = string.Empty;

    public SolutionPatcherNameVm(
        IProjectSubpathDefaultSettings defaultSettings,
        SolutionNameConstructor nameConstructor,
        ISelectedProjectInputVm selectedProjectInputVm)
    {
        _name = selectedProjectInputVm.WhenAnyValue(x => x.Picker.TargetPath)
            .CombineLatest(
                this.WhenAnyValue(x => x.Nickname),
                (path, nickname) => nameConstructor.Construct(nickname, path))
            .ToGuiProperty<string>(this, nameof(Name), nameConstructor.Construct(Nickname, defaultSettings.ProjectSubpath), deferSubscription: true);
    }
}