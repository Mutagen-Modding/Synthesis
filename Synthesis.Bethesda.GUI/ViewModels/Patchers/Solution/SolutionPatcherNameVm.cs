using System;
using System.IO;
using System.Reactive.Linq;
using Noggog.WPF;
using ReactiveUI;
using Synthesis.Bethesda.Execution.Patchers.Solution;
using Synthesis.Bethesda.GUI.Services.Patchers.Solution;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.TopLevel;

namespace Synthesis.Bethesda.GUI.ViewModels.Patchers.Solution
{
    public class SolutionPatcherNameVm : ViewModel, IPatcherNameVm
    {
        private readonly ObservableAsPropertyHelper<string> _Name;
        public string Name => _Name.Value;

        public SolutionPatcherNameVm(
            IProjectSubpathDefaultSettings defaultSettings,
            ISelectedProjectInputVm selectedProjectInputVm)
        {
            _Name = selectedProjectInputVm.WhenAnyValue(x => x.Picker.TargetPath)
                .Select(GetNameFromPath)
                .ToGuiProperty<string>(this, nameof(Name), GetNameFromPath(defaultSettings.ProjectSubpath));
        }

        private string GetNameFromPath(string path)
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
        }
    }
}