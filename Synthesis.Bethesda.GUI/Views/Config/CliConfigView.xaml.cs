using Noggog.WPF;
using System;
using ReactiveUI;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Synthesis.Bethesda.GUI.ViewModels.Patchers;

namespace Synthesis.Bethesda.GUI.Views
{
    public class CliConfigViewBase : NoggogUserControl<CliPatcherVm> { }

    /// <summary>
    /// Interaction logic for CliConfigView.xaml
    /// </summary>
    public partial class CliConfigView : CliConfigViewBase
    {
        public CliConfigView()
        {
            InitializeComponent();
            this.WhenActivated(disposable =>
            {
                this.BindStrict(this.ViewModel, vm => vm.PathToExecutable, view => view.ExecutablePathPicker.PickerVM)
                    .DisposeWith(disposable);

                // ToDo
                // Re-enable
                
                // var isNewPatcher = this.WhenAnyFallback(x => x.ViewModel!.Profile.Config.NewPatcher, default)
                //     .Select(newPatcher => newPatcher != null)
                //     .Replay(1)
                //     .RefCount();
                
                // Hide help box if not in initialization
                UtilityBindings.HelpWiring(this.ViewModel!.ShowHelpSetting, this.HelpButton, this.HelpText)
                    .DisposeWith(disposable);
            });
        }
    }
}
