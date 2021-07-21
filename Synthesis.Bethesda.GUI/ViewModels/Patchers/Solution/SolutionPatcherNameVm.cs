using System;
using System.IO;
using System.Reactive.Linq;
using Noggog.WPF;
using ReactiveUI;
using Synthesis.Bethesda.GUI.Services.Patchers.Solution;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.TopLevel;

namespace Synthesis.Bethesda.GUI.ViewModels.Patchers.Solution
{
    public class SolutionPatcherNameVm : ViewModel, IPatcherNameVm
    {
        private readonly ObservableAsPropertyHelper<string> _Name;
        public string Name => _Name.Value;

        public SolutionPatcherNameVm(ISelectedProjectInputVm selectedProjectInputVm)
        {
            _Name = selectedProjectInputVm.WhenAnyValue(x => x.Picker.TargetPath)
                .Select((path) =>
                {
                    try
                    {
                        var name = Path.GetFileName(Path.GetDirectoryName(path));
                        if (string.IsNullOrWhiteSpace(name)) return string.Empty;
                        return name;
                    }
                    catch (Exception)
                    {
                        return string.Empty;
                    }
                })
                .ToGuiProperty<string>(this, nameof(Name), selectedProjectInputVm.Picker.TargetPath);
        }
    }
}