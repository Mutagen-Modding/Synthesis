using Noggog.WPF;
using ReactiveUI;
using System.Reactive.Disposables;
using System;
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
using Synthesis.Bethesda.GUI.ViewModels.Profiles.Running;

namespace Synthesis.Bethesda.GUI.Views
{
    public class PatcherRunListingViewBase : NoggogUserControl<PatcherRunVm> { }

    /// <summary>
    /// Interaction logic for PatcherRunListingView.xaml
    /// </summary>
    public partial class PatcherRunListingView : PatcherRunListingViewBase
    {
        public PatcherRunListingView()
        {
            InitializeComponent();
            this.WhenActivated(disposable =>
            {
                this.WhenAnyValue(x => x.ViewModel!.Config.NameVm.Name)
                    .BindToStrict(this, x => x.NameBlock.Text)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.IsSelected)
                    .Select(x => x ? Visibility.Visible : Visibility.Collapsed)
                    .BindToStrict(this, x => x.SelectedGlow.Visibility)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel!.RunTimeString)
                    .BindToStrict(this, x => x.RunningTimeBlock.Text)
                    .DisposeWith(disposable);
            });
        }
    }
}
