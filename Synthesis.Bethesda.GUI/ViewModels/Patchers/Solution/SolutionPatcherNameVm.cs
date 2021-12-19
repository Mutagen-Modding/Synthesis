using System;
using System.IO;
using System.Reactive.Linq;
using Noggog;
using Noggog.WPF;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Synthesis.Bethesda.Execution.Patchers.Solution;
using Synthesis.Bethesda.GUI.Services.Patchers.Solution;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.TopLevel;

namespace Synthesis.Bethesda.GUI.ViewModels.Patchers.Solution
{
    public class SolutionPatcherNameVm : ViewModel, IPatcherNameVm
    {
        private readonly ObservableAsPropertyHelper<string> _name;
        public string Name => _name.Value;

        [Reactive] public string Nickname { get; set; } = string.Empty;

        public SolutionPatcherNameVm(
            IProjectSubpathDefaultSettings defaultSettings,
            ISelectedProjectInputVm selectedProjectInputVm)
        {
            _name = selectedProjectInputVm.WhenAnyValue(x => x.Picker.TargetPath)
                .Select(GetNameFromPath)
                .CombineLatest(
                    this.WhenAnyValue(x => x.Nickname),
                    (auto, nickname) => nickname.IsNullOrWhitespace() ? auto : nickname)
                .ToGuiProperty<string>(this, nameof(Name), GetNameFromPath(defaultSettings.ProjectSubpath), deferSubscription: true);
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