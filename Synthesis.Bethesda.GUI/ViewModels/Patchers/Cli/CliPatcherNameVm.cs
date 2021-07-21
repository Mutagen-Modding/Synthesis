using System;
using System.IO;
using Noggog.WPF;
using ReactiveUI;
using System.Reactive.Linq;
using Synthesis.Bethesda.GUI.Services.Patchers.Cli;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.TopLevel;

namespace Synthesis.Bethesda.GUI.ViewModels.Patchers.Cli
{
    public class CliPatcherNameVm : ViewModel, IPatcherNameVm
    {
        private readonly ObservableAsPropertyHelper<string> _Name;
        public string Name => _Name.Value;

        public CliPatcherNameVm(IPathToExecutableInputVm pathToExecutableInputVm)
        {
            _Name = pathToExecutableInputVm.WhenAnyValue(x => x.Picker.TargetPath)
                .Select(x =>
                {
                    try
                    {
                        return Path.GetFileNameWithoutExtension(x);
                    }
                    catch (Exception)
                    {
                        return "<Naming Error>";
                    }
                })
                .ToGuiProperty<string>(this, nameof(Name), string.Empty);
        }
    }
}