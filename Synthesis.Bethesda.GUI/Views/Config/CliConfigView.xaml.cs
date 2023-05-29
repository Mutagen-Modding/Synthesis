using ReactiveUI;
using System.Reactive.Disposables;

namespace Synthesis.Bethesda.GUI.Views;

/// <summary>
/// Interaction logic for CliConfigView.xaml
/// </summary>
public partial class CliConfigView
{
    public CliConfigView()
    {
        InitializeComponent();
        this.WhenActivated(disposable =>
        {
            this.Bind(this.ViewModel, vm => vm.ExecutableInput.Picker, view => view.ExecutablePathPicker.PickerVM)
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