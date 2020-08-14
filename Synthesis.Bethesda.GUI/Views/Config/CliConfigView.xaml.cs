using Noggog.WPF;
using System;
using ReactiveUI;
using System.Reactive.Disposables;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Reactive.Linq;

namespace Synthesis.Bethesda.GUI.Views
{
    public class CliConfigViewBase : NoggogUserControl<CliPatcherVM> { }

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

                // Hide help box if not in initialization
                UtilityBindings.HelpWiring(this.ViewModel, this.HelpButton, this.HelpText)
                    .DisposeWith(disposable);
            });
        }
    }
}
